@description('The name of the virtual machine')
param vmName string = 'whmcs-worker-vm'

@description('The location for all resources')
param location string = resourceGroup().location

@description('Admin username for the virtual machine')
param adminUsername string = 'azureuser'

@description('SSH public key for the virtual machine admin user')
@secure()
param adminSshPublicKey string

@description('The name of the virtual network')
param vnetName string = '${vmName}-vnet'

@description('The name of the subnet')
param subnetName string = '${vmName}-subnet'

@description('The name of the network security group')
param nsgName string = '${vmName}-nsg'

@description('The name of the network interface')
param nicName string = '${vmName}-nic'

@description('The name of the public IP address')
param publicIpName string = '${vmName}-pip'

@description('The name of the OS disk')
param osDiskName string = '${vmName}-osdisk'

// Cloud-init script that runs on first boot to install the .NET 10 runtime and
// create the whmcsworker system user with the directory structure expected by the
// WhmcsWorkerService and its systemd unit file.
var cloudInitScript = '''#cloud-config
package_update: true
runcmd:
  - wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
  - dpkg -i /tmp/packages-microsoft-prod.deb
  - rm /tmp/packages-microsoft-prod.deb
  - apt-get update -y
  - apt-get install -y aspnetcore-runtime-10.0 unzip
  - getent passwd whmcsworker > /dev/null 2>&1 || useradd --system --no-create-home --shell /usr/sbin/nologin whmcsworker
  - mkdir -p /opt/whmcs-worker /etc/whmcs-worker /var/log/whmcs-worker
  - chown -R whmcsworker:whmcsworker /opt/whmcs-worker /var/log/whmcs-worker
  - chown root:root /etc/whmcs-worker
  - chmod 750 /etc/whmcs-worker
'''

// Static Public IP for outbound WHMCS API calls (required for IP allowlisting)
resource publicIp 'Microsoft.Network/publicIPAddresses@2024-01-01' = {
  name: publicIpName
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
    publicIPAddressVersion: 'IPv4'
  }
}

// Network Security Group – restrict inbound SSH; outbound traffic uses default NSG allow rules
resource nsg 'Microsoft.Network/networkSecurityGroups@2024-01-01' = {
  name: nsgName
  location: location
  properties: {
    securityRules: [
      {
        name: 'Allow-SSH-Inbound'
        properties: {
          priority: 100
          protocol: 'Tcp'
          access: 'Allow'
          direction: 'Inbound'
          sourceAddressPrefix: 'VirtualNetwork'
          sourcePortRange: '*'
          destinationAddressPrefix: '*'
          destinationPortRange: '22'
        }
      }
      {
        name: 'Deny-All-Other-Inbound'
        properties: {
          priority: 4096
          protocol: '*'
          access: 'Deny'
          direction: 'Inbound'
          sourceAddressPrefix: '*'
          sourcePortRange: '*'
          destinationAddressPrefix: '*'
          destinationPortRange: '*'
        }
      }
    ]
  }
}

// Virtual Network
resource vnet 'Microsoft.Network/virtualNetworks@2024-01-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: ['10.0.0.0/24']
    }
    subnets: [
      {
        name: subnetName
        properties: {
          addressPrefix: '10.0.0.0/24'
          networkSecurityGroup: {
            id: nsg.id
          }
        }
      }
    ]
  }
}

// Network Interface
resource nic 'Microsoft.Network/networkInterfaces@2024-01-01' = {
  name: nicName
  location: location
  properties: {
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          subnet: {
            id: '${vnet.id}/subnets/${subnetName}'
          }
          privateIPAllocationMethod: 'Dynamic'
          publicIPAddress: {
            id: publicIp.id
          }
        }
      }
    ]
  }
}

// Virtual Machine – Standard_B1ls is the lowest-cost Azure Linux VM SKU
// (1 vCPU, 0.5 GiB RAM; Linux only)
resource vm 'Microsoft.Compute/virtualMachines@2024-07-01' = {
  name: vmName
  location: location
  properties: {
    hardwareProfile: {
      vmSize: 'Standard_B1ls'
    }
    storageProfile: {
      imageReference: {
        // Ubuntu 22.04 LTS – small footprint, long-term support
        publisher: 'Canonical'
        offer: '0001-com-ubuntu-server-jammy'
        sku: '22_04-lts-gen2'
        version: 'latest'
      }
      osDisk: {
        name: osDiskName
        createOption: 'FromImage'
        managedDisk: {
          storageAccountType: 'Standard_LRS'
        }
        diskSizeGB: 30
        deleteOption: 'Delete'
      }
    }
    osProfile: {
      computerName: vmName
      adminUsername: adminUsername
      linuxConfiguration: {
        disablePasswordAuthentication: true
        ssh: {
          publicKeys: [
            {
              path: '/home/${adminUsername}/.ssh/authorized_keys'
              keyData: adminSshPublicKey
            }
          ]
        }
      }
      customData: base64(cloudInitScript)
    }
    networkProfile: {
      networkInterfaces: [
        {
          id: nic.id
          properties: {
            deleteOption: 'Delete'
          }
        }
      ]
    }
    diagnosticsProfile: {
      bootDiagnostics: {
        enabled: false
      }
    }
  }
}

@description('The static public IP address of the VM (use this for WHMCS IP allowlisting)')
output staticPublicIpAddress string = publicIp.properties.ipAddress

@description('The resource ID of the virtual machine')
output vmId string = vm.id

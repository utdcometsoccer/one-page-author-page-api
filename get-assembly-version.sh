#!/bin/bash
#
# get-assembly-version.sh - Extract version information from .NET assemblies
#
# Usage:
#   ./get-assembly-version.sh <path-to-dll>
#   ./get-assembly-version.sh <directory>
#
# Examples:
#   ./get-assembly-version.sh function-app/bin/Release/net10.0/function-app.dll
#   ./get-assembly-version.sh function-app/bin/Release/net10.0/
#

set -e

show_usage() {
    echo "Usage: $0 <path-to-dll-or-directory>"
    echo ""
    echo "Extract version information from .NET assemblies."
    echo ""
    echo "Examples:"
    echo "  $0 function-app/bin/Release/net10.0/function-app.dll"
    echo "  $0 function-app/bin/Release/net10.0/"
}

extract_version() {
    local dll_file="$1"
    local dll_name=$(basename "$dll_file")
    
    echo ""
    echo "Assembly: $dll_name"
    echo "----------------------------------------"
    
    # Extract version strings from the binary
    # Note: Some versions may have a prefix character, so we strip it
    local versions=$(strings "$dll_file" | grep -E "[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?(\+sha\.[a-f0-9]+)?" | grep -E "^.?[0-9]+\.[0-9]+\.[0-9]+" | sed 's/^[^0-9]*//' | sort -u)
    
    if [ -z "$versions" ]; then
        echo "  No version information found"
        return
    fi
    
    # Display found versions
    echo "$versions" | while read -r version; do
        if [[ "$version" =~ \+ ]]; then
            echo "  InformationalVersion: $version"
        elif [[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
            echo "  FileVersion:          $version"
        else
            echo "  Version:              $version"
        fi
    done
    
    echo "  Path: $dll_file"
}

# Main script
if [ $# -eq 0 ]; then
    show_usage
    exit 1
fi

path="$1"

if [ ! -e "$path" ]; then
    echo "Error: Path does not exist: $path"
    exit 1
fi

if [ -f "$path" ]; then
    # Single file
    if [[ "$path" == *.dll ]]; then
        extract_version "$path"
    else
        echo "Error: File is not a DLL: $path"
        exit 1
    fi
elif [ -d "$path" ]; then
    # Directory - process all DLL files
    echo "Scanning directory: $path"
    
    dll_count=$(find "$path" -maxdepth 1 -name "*.dll" -type f | wc -l)
    
    if [ "$dll_count" -eq 0 ]; then
        echo "No DLL files found in: $path"
        exit 0
    fi
    
    echo "Found $dll_count assemblies"
    
    find "$path" -maxdepth 1 -name "*.dll" -type f | sort | while read -r dll; do
        extract_version "$dll"
    done
else
    echo "Error: Path is not a file or directory: $path"
    exit 1
fi

echo ""

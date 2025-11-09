# Amazon Associate Partner Tag Finder Guide

## 🎯 Quick Steps to Find Your Partner Tag

### 1. **Access Amazon Associates Central**
```
URL: https://affiliate-program.amazon.com/
```
- Sign in with your Amazon Associates account
- If you don't have one, apply first (approval required)

### 2. **Locate Your Associate ID**

#### Option A: Dashboard Method
1. After login, look at your main dashboard
2. Find "Associate ID" or "Tracking ID" 
3. Copy the value (format: `yourstore-20`)

#### Option B: Account Settings Method
1. Go to **"Account & Login Info"**
2. Click **"Manage Your Tracking IDs"**
3. All your Associate IDs will be listed
4. Copy the one you want to use

#### Option C: Link Generator Method
1. Go to **"Product Linking"** → **"Link to Any Page"**
2. Generate any affiliate link
3. Your Associate ID appears as `tag=yourstore-20` in the URL

### 3. **Verify Tag Format**
✅ **Correct Format**: `storename-20`, `mybooks-21`, `techstore-20`
❌ **Wrong Format**: `storename`, `20-storename`, `storename_20`

### 4. **Regional Suffixes**
- 🇺🇸 **US**: `-20` (amazon.com)
- 🇬🇧 **UK**: `-21` (amazon.co.uk)  
- 🇩🇪 **Germany**: `-03` (amazon.de)
- 🇫🇷 **France**: `-21` (amazon.fr)
- 🇯🇵 **Japan**: `-22` (amazon.co.jp)
- 🇨🇦 **Canada**: `-20` (amazon.ca)

## ⚠️ IMPORTANT: Product Advertising API Access

**Having an Amazon Associates account is NOT enough!**

You need **separate approval** for Product Advertising API:

### 1. Apply for PA API Access
```
URL: https://developer.amazon.com/
```
1. Sign in to Amazon Developer Portal
2. Navigate to **"Product Advertising API"**
3. Click **"Request Access"** 
4. Fill out application form
5. Wait for approval (can take days/weeks)

### 2. Generate AWS Credentials
After PA API approval:
1. Go to **"Manage Your Apps"** in Developer Portal
2. Create or select your PA API application
3. Generate **Access Key** and **Secret Key**
4. These are your `AMAZON_PRODUCT_ACCESS_KEY` and `AMAZON_PRODUCT_SECRET_KEY`

## 🔧 Update Your Configuration

Once you have your real Partner Tag:

```bash
# Update user secrets
dotnet user-secrets set "AMAZON_PRODUCT_PARTNER_TAG" "your-real-tag-20" --project InkStainedWretchFunctions\InkStainedWretchFunctions.csproj

# Test the configuration
dotnet run --project AmazonProductTestConsole\AmazonProductTestConsole.csproj -- --config
```

## 🚨 Common Issues

### Issue: 404 Error with Valid Partner Tag
**Cause**: Associates account exists but no PA API approval
**Solution**: Apply for Product Advertising API access separately

### Issue: Can't Find Partner Tag
**Cause**: No Amazon Associates account
**Solution**: 
1. Apply at https://affiliate-program.amazon.com/
2. Complete profile and tax information
3. Wait for approval
4. Then apply for PA API access

### Issue: 403 Forbidden Error  
**Cause**: Invalid AWS credentials or wrong region
**Solution**: 
1. Verify Access Key and Secret Key from Developer Portal
2. Ensure region matches your PA API setup
3. Check that credentials have PA API permissions

## 📞 Support Resources

- **Amazon Associates Help**: https://affiliate-program.amazon.com/help/
- **PA API Documentation**: https://webservices.amazon.com/paapi5/documentation/
- **Developer Portal**: https://developer.amazon.com/apps-and-games/services/paapi

## ✅ Verification Steps

1. **Check Associate Account**: Login to affiliate-program.amazon.com
2. **Find Partner Tag**: Look for Associate ID/Tracking ID
3. **Verify PA API Access**: Check developer.amazon.com for approved applications
4. **Test Configuration**: Use the console app to verify API calls
5. **Monitor Logs**: Check debug output for signature validation

Your Partner Tag should work once both your Associates account and Product Advertising API access are approved!

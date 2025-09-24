# Image API Documentation

This API allows users to upload images to Azure Blob Storage, retrieve a list of their uploaded images, and enforces file size and file count limits based on subscription tier.

## Service Tiers

### Starter (Free)
- **Storage:** 5GB
- **Bandwidth:** 25GB
- **Max file size:** 5MB
- **Max files:** 20

### Pro ($9.99/month)
- **Storage:** 250GB
- **Bandwidth:** 1TB
- **Max file size:** 10MB
- **Max files:** 500

### Elite ($19.99/month)
- **Storage:** 2TB
- **Bandwidth:** 10TB
- **Max file size:** 25MB
- **Max files:** 2000

## Endpoints

### 1. Upload Image
- **POST** `/api/images/upload`
- **Description:** Upload an image file to Azure Blob Storage.
- **Headers:**
  - `Authorization: Bearer <token>`
- **Body:**
  - `file`: image file (multipart/form-data)
- **Limits:**
  - File size and count limits depend on subscription tier (see Service Tiers above)
- **Responses:**
  - `201 Created`: Image uploaded successfully. Returns `{ id, url, name, size }`
  - `400 Bad Request`: File too large for subscription tier. Returns `{ error: "File size exceeds limit for your subscription tier." }`
  - `403 Forbidden`: User has reached upload limit for their tier. Returns `{ error: "Maximum number of files reached for your subscription tier." }`
  - `402 Payment Required`: Bandwidth limit exceeded. Returns `{ error: "Bandwidth limit exceeded for your subscription tier." }`
  - `507 Insufficient Storage`: Storage quota exceeded. Returns `{ error: "Storage quota exceeded for your subscription tier." }`
  - `401 Unauthorized`: Invalid or missing token.

### 2. List User Images
- **GET** `/api/images/user`
- **Description:** Get a list of all images uploaded by the authenticated user.
- **Headers:**
  - `Authorization: Bearer <token>`
- **Responses:**
  - `200 OK`: Returns `[ { id, url, name, size, uploadedAt } ]`
  - `401 Unauthorized`: Invalid or missing token.

### 3. Delete Image
- **DELETE** `/api/images/{id}`
- **Description:** Delete an image by its ID.
- **Headers:**
  - `Authorization: Bearer <token>`
- **Responses:**
  - `200 OK`: Image deleted successfully.
  - `404 Not Found`: Image not found.
  - `401 Unauthorized`: Invalid or missing token.

## Error Codes
- `400`: Bad Request (e.g., file too large for subscription tier)
- `401`: Unauthorized
- `402`: Payment Required (e.g., bandwidth limit exceeded)
- `403`: Forbidden (e.g., upload limit reached for subscription tier)
- `404`: Not Found
- `507`: Insufficient Storage (storage quota exceeded)

## Notes
- All endpoints require authentication.
- Images are stored in Azure Blob Storage and returned with public URLs.
- File size, file count, storage, and bandwidth limits are enforced based on subscription tier.
- Users can upgrade their subscription tier to access higher limits.

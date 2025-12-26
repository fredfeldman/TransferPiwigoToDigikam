# Setup Guide - Complete These Steps

## ?? Important: Required Setup Steps

The application has been created, but you need to complete these steps to make it buildable:

### Step 1: Close and Reopen the Solution
Visual Studio needs to reload the project to recognize new files.
1. Save all files
2. Close the solution (File > Close Solution)
3. Reopen the solution

### Step 2: Install NuGet Package
Install the System.Data.SQLite package:

**Option A - NuGet Package Manager Console:**
```
Install-Package System.Data.SQLite.Core
```

**Option B - NuGet Package Manager UI:**
1. Right-click on the project "TransferPiwigoToDigikam"
2. Select "Manage NuGet Packages"
3. Click the "Browse" tab
4. Search for "System.Data.SQLite.Core"
5. Click Install

### Step 3: Add System.Web.Extensions Reference
1. Right-click on "References" in Solution Explorer
2. Click "Add Reference"
3. In the "Assemblies" section, find "System.Web.Extensions"
4. Check the box next to it
5. Click OK

### Step 4: Verify New Files Are Included
Make sure these files are in your project:
- ? Models\PiwigoImage.cs
- ? Services\PiwigoClient.cs
- ? Services\DigiKamExporter.cs
- ? Services\ImageTransferService.cs

If any are missing:
1. Right-click on the project
2. Select "Add" > "Existing Item"
3. Navigate to and select the missing file(s)

### Step 5: Build the Project
1. Build > Rebuild Solution
2. Verify there are no errors

## Files Created

### Core Application Files
- **Form1.cs** - Main UI form with transfer logic
- **Form1.Designer.cs** - UI layout and controls

### Business Logic Files
- **Models/PiwigoImage.cs** - Data models for Piwigo images and categories
- **Services/PiwigoClient.cs** - Handles communication with Piwigo API
- **Services/DigiKamExporter.cs** - Creates DigiKam database and saves images
- **Services/ImageTransferService.cs** - Orchestrates the transfer process

### Configuration Files
- **packages.config** - NuGet package configuration
- **README.md** - User documentation

## What the Application Does

1. **Connects to Piwigo** - Authenticates with your Piwigo website
2. **Retrieves Categories** - Gets all albums/categories from Piwigo
3. **Downloads Images** - Downloads all images with metadata
4. **Creates DigiKam Structure** - Organizes files in folders
5. **Builds Database** - Creates SQLite database for DigiKam
6. **Preserves Metadata** - Keeps tags, comments, dates, etc.

## Quick Start After Setup

1. Run the application
2. Enter:
   - Piwigo URL (e.g., https://photos.example.com)
   - Username
   - Password
   - Output directory (where to save the collection)
3. Click "Start Transfer"
4. Monitor progress in the status window

## Opening in DigiKam

After transfer completes:
1. Open DigiKam
2. Settings > Configure DigiKam > Collections
3. Add New Collection
4. Point to your output directory
5. DigiKam will automatically detect the database

## Troubleshooting

### If Build Still Fails

**Missing System.Data.SQLite:**
- Reinstall the NuGet package
- Check that packages are restored (right-click solution > Restore NuGet Packages)

**Missing System.Web.Extensions:**
- Add reference manually (see Step 3 above)
- This is a standard .NET Framework assembly

**Files Not Included:**
- Use "Show All Files" in Solution Explorer
- Right-click on the folders/files
- Select "Include In Project"

### Runtime Errors

**"Could not load file or assembly 'System.Data.SQLite'"**
- The SQLite native DLLs need to be copied to output
- This should happen automatically with the NuGet package
- If not, check the packages folder for x86/x64 folders

**Connection Failed:**
- Verify Piwigo URL is correct
- Check username/password
- Ensure API access is enabled on your Piwigo site

## Technical Architecture

```
Form1 (UI)
    ??? ImageTransferService (Orchestrator)
            ??? PiwigoClient (API Communication)
            ??? DigiKamExporter (Database & File Management)
                    ??? Models (Data Structures)
```

## Need Help?

Check the main README.md file for detailed documentation about:
- Features
- Usage instructions  
- DigiKam database structure
- API methods used
- Troubleshooting tips

[![Build Status](https://dev.azure.com/ourstorytitan/OurStoryBuilds/_apis/build/status/our-story-media.ourstory-desktop?branchName=master)](https://dev.azure.com/ourstorytitan/OurStoryBuilds/_build/latest?definitionId=8&branchName=master)

# Our Story Local Sync Client

This software allows you to keep a local directory up to date with a particular Our Story shoot.

You will need the following to get setup:

- Administrator rights on an Our Story shoot
- Live internet connection

## Usage

1. Open Our Story Sync Client (and allow ports to be opened locally if requested)
1. Login to Our Story and go to the Export panel of your chosen shoot.
1. Select which export template you wish to apply.
1. Click 'Connect to Sync Client'
1. Return to Our Story Sync Client
1. Select options and press Activate Sync

## Notes
- Transcode to HD will output 1920 x 1080 mp4 clips with identical bit rates and framerates, and is helpful for editing. ALL clips will be scaled to this resolution, regardless of the input format. When this option is not selected, original clips are downloaded.
- XMP meta-data is applied using a third-party tool (exiftool) which makes a backup of each file, thus doubling the download size.
- Downloads can be resumed at a later time by closing the application and performing the same connection process. Downloads wil resume where they were left.
# YoutubeTagger
Downloads youtube videos and tags the audio files based on an xml document. Allows for embedding thumbnails and running from command line with exit codes

## Automation
You can modify the `DownloadInfo.xml` document to control how YoutubeTager behaves. For documentation and examples, see https://github.com/Willster419/YoutubeTagger/tree/master/YoutubeTagger/bin/Debug

For example, you can have it ask for each step and show errors. When you're satisfied with your setup, you can set it to run automatically without user intervention. If an error occures, you can have the application stop, or exit early with a bad (not 0) exit code.

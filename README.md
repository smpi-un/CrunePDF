# CrunePDF

Rotate each page of the PDF in a direction where the text can be read in Windows.

# DEMO
![demo](https://github.com/smpi-un/crunepdf/blob/main/doc/demo.gif)

# Installation

1. Download the binary file from the release page.
1. Unzip the downloaded file to any folder.

# Usage

Drag and drop PDF files into the program.

Or call the program from the command line.
```
> crunepdf Target.pdf
```

The default behavior such as output directory, language, etc. can be changed in the configuration file.
Sample:
```json
{
  "language": "en",
  "raitoThreshold": 1.25,
  "scoreThreshold": 10,
  "outputDirectory": "%UserProfile%\\Desktop"
}
```

# Author

* Shimpei Ueno
* Densan Club(Working circle in Toyama, Japan)
* https://twitter.com/densanclub

# License

"CrunePDF" is under [MIT license](https://github.com/smpi-un/crunepdf/blob/main/LICENSE).

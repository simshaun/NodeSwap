# NodeSwap

NodeSwap is a version manager for [Node.js](https://nodejs.org/), similar to NVM.
Built for Windows.

## Installing and Updating

> ### ⚠ Prerequisite: 
> **Existing Node.js installations:**  
> - Be sure to uninstall any existing Node.js installations.
> - Remove `%AppData%\npm` to prevent global module conflicts.

### Install NodeSwap
1. Ensure `C:\Program Files\nodejs` is in your PATH environment variable.
2. Download and run the latest installer from the Releases page.

### Upgrade NodeSwap
Download and run the latest installer from the Releases page.


## Usage

> ### ⚠ Admin rights: 
> **NodeSwap** needs to be ran in an elevated terminal (i.e. Run as Administrator).  
> It needs this in order to create symlinks when installing and swapping Node.js versions. 

Type `nodeswap` in your terminal for help.

### Commands:

- `nodeswap` — Provides an overview of commands
- `nodeswap avail [<min>]` — List the versions available for download. A minimum
                             version can be specified to reduce the amount of output.
- `nodeswap install <version>` — The version can be `latest`, a specific version
                                 like `14.16.1`, or a fuzzy version like `14.16` or `14`.
- `nodeswap uninstall <version>` — The version must be specific like `14.16.1`.
- `nodeswap use <version>` — Switch to a specific version. Must be specific like `14.16.1`.

## Caveats

- This software is in early stages. There are some features missing such as
  the ability to swap between 32-bit and 64-bit Node.js versions.

## Issues & Feature Requests

- Please report in the [Issue Tracker](https://github.com/FoxAndFly/NodeSwap/issues).

## License

MIT

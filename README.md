# MSIAutoTweak Documentation

## Overview

By default, Windows 11 assigns interrupts from devices to already active P-cores on Intel hybrid CPUs, and not to sleeping E-cores which already have nothing to do. This can lead to increased latency and stuttering. Also Windows does not enforce using MSI interrupts for devices that support them, instead assigns the archaic line base interrupts to devices.
MSIAutoTweak is a little Windows tool designed to optimize interrupt cpu allocation on Windows 11 systems with Intel hybrid CPUs (featuring P-cores and E-cores). It is inspired by other similar manual interrupt affinity tools, but I wanted to create a simple, automated solution that does not require manual configuration or knowledge of which devices to tweak. It automatically enables Message Signaled Interrupts (MSI) for devices that support it and assigns their interrupts to E-cores, potentially improving system performance for latency-sensitive tasks.
Note: You have to rerun this after every driver update as these restore the default not optimized settings for that device.

## Installation

### Prerequisites

- Windows 10 or later.
- .NET 8 runtime  or .NET 9 runtime (optional).
- Administrator privileges for installation and usage.

### Using the Pre-Built Installer

1. Download the installer `MSIAutoTweak-net8.exe` from Releases. Alternatively, if you have .NET 9 installed (optional), you can use `MSIAutoTweak-net9.exe`.
2. Run the installer as administrator.
3. Follow the wizard:
   - Installs to `C:\Program Files\MSIAutoTweak`.
4. Start the application from the Start menu or by running `MSIAutoTweak.exe` from the installation directory.

## License

This project is licensed under the GNU General Public License v3.0. See the [LICENSE](LICENSE) file for details.

## Contributing

- Submit issues or pull requests to the repository (if applicable).

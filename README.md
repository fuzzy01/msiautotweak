# MSIAutoTweak Documentation

## Overview

By default, Windows 11 assigns interrupts from devices to already active P-cores on Intel hybrid CPUs, and not to E-cores (efficiency cores) which already have nothing to do. Also Windows does not enforce using MSI interrupts for devices that support them, instead falling back to the archaic line-based interrupts.

MSIAutoTweak is a Windows tool designed to optimize interrupt CPU allocation on systems with Intel hybrid CPUs (featuring P-cores and E-cores). It is inspired by other similar manual interrupt affinity tools, but provides a simple automated solution that requires no manual configuration or knowledge of which devices to tweak. It automatically enables Message Signaled Interrupts (MSI) for devices that support it and assigns their interrupts to the appropriate cores based on the selected optimization strategy.

**Note:** You have to rerun this after every driver update, as driver updates restore the default settings for that device. If the GPU configuration is changed by this tool (e.g., by changing the number of video cores), you also need to restart the computer.

### Optimization Strategies

- **Default** — Resets all devices to Windows machine default interrupt routing.
- **Move to E-cores** — Assigns all device interrupts to E-cores, keeping P-cores free for application workloads.
- **Hybrid** — Assigns USB and display device interrupts to P-cores, and all other devices to E-cores.

### Options

- **Strategy** — Select the optimization strategy (see above).
- **Video cores** — Number of interrupt vectors (cores) assigned to display (GPU) devices. Also writes `MessageNumberLimit` to the registry for that device. Default: 1.
- **Restart devices** — Restart devices after applying settings so changes take effect immediately. May cause a brief screen flicker.
- **Optimize misc devices** — Also assign remaining MSI-capable and line-based devices to available cores.

<img width="800" alt="image" src="https://github.com/user-attachments/assets/9196d06f-507c-4ce9-9b3c-4c9731a783cf" />

## Installation

### Prerequisites

- Windows 10 or later.
- .NET 8 runtime or .NET 10 runtime (optional).
- Administrator privileges for installation and usage.

### Using the Pre-Built Installer

1. Download the installer `MSIAutoTweak-net8.exe` from Releases. Alternatively, if you have .NET 10 installed (optional), you can use `MSIAutoTweak-net10.exe`.
2. Run the installer as administrator.
3. Follow the wizard:
   - Installs to `C:\Program Files\MSIAutoTweak`.
4. Start the application from the Start menu or by running `MSIAutoTweak.exe` from the installation directory.

## License

This project is licensed under the GNU General Public License v3.0. See the [LICENSE](LICENSE) file for details.

## Contributing

- Submit issues or pull requests to the repository (if applicable).

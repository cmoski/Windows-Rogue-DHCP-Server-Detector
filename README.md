# Windows Rogue DHCP Server Detector

## Overview
Windows Rogue DHCP Server Detector is a tool designed to identify and monitor unauthorized DHCP servers on Windows networks. Rogue DHCP servers can cause significant network disruptions, including IP address conflicts, incorrect network configurations, and potential security vulnerabilities.

## What is a Rogue DHCP Server?
A rogue DHCP server is any unauthorized DHCP server on your network that is not under management control. These can appear when:
- Users accidentally connect consumer-grade routers with DHCP enabled
- Malicious actors introduce DHCP servers for man-in-the-middle attacks
- Systems are misconfigured with DHCP services enabled
- Malware deploys DHCP services to capture network traffic

## Key Features
- Detects unauthorized DHCP servers on local subnets
- Monitors multiple network interfaces
- Configurable scanning intervals
- System tray integration for continuous monitoring
- Detailed reporting of discovered DHCP servers
- Comparison with authorized DHCP server list
- Minimal resource usage

## Requirements
- Windows operating system
- .NET Framework 4.5 or higher
- Administrative privileges (for network scanning)

## Installation
1. Download the latest release from the [Releases](https://github.com/cmoski/Windows-Rogue-DHCP-Server-Detector/releases) page
2. Extract the ZIP file to your preferred location
3. Run `RogueDHCPDetector.exe` as an administrator

## Usage
1. Launch the application with administrative privileges
2. Select the network interface(s) to monitor
3. Click "Detect Rogue Servers" to perform an immediate scan
4. Configure automatic scanning interval if desired
5. View results in the main window

### Scheduled Monitoring
The detector can be configured to run automatically at specified intervals:
1. Set your desired scan frequency in the Settings menu
2. The application will minimize to the system tray
3. Notifications will appear when rogue servers are detected

## How It Works
The Windows Rogue DHCP Server Detector works by:
1. Sending DHCP discovery packets across selected network interfaces
2. Collecting and analyzing responses from DHCP servers
3. Comparing discovered servers against authorized server list
4. Identifying unauthorized/rogue DHCP servers
5. Providing detailed information including IP and MAC addresses

## Troubleshooting
- **No DHCP servers detected**: Ensure you're running as administrator and have selected the correct network interface
- **False positives**: Update your authorized DHCP server list in settings
- **Performance issues**: Adjust scanning frequency to reduce network load

## Contributing
Contributions to improve the Windows Rogue DHCP Server Detector are welcome! Please feel free to submit a pull request or create an issue to discuss proposed changes.

## License
This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments
- Based on Microsoft's original Rogue DHCP Server detection concept
- Thanks to all contributors who have helped improve this tool

## Security Considerations
While this tool helps identify rogue DHCP servers, it should be part of a comprehensive network security strategy. Consider implementing DHCP snooping on managed switches for persistent protection against unauthorized DHCP servers.

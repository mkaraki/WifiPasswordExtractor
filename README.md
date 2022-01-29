# WifiPasswordExtractor

[![Codacy Badge](https://api.codacy.com/project/badge/Grade/90136e9b610f4d20b2593fe6957feb7f)](https://app.codacy.com/gh/mkaraki/WifiPasswordExtractor?utm_source=github.com&utm_medium=referral&utm_content=mkaraki/WifiPasswordExtractor&utm_campaign=Badge_Grade_Settings)
[![WifiPasswordExtractor](https://github.com/mkaraki/WifiPasswordExtractor/actions/workflows/build-cmd.yml/badge.svg)](https://github.com/mkaraki/WifiPasswordExtractor/actions/workflows/build-cmd.yml)
[![WifiPasswordExtractorGUI](https://github.com/mkaraki/WifiPasswordExtractor/actions/workflows/build-desktop.yml/badge.svg)](https://github.com/mkaraki/WifiPasswordExtractor/actions/workflows/build-desktop.yml)

Wi-Fi Password Extractor for Windows

## Supported Wi-Fi Security
- Open
- WEP
- WPA Personal
- WPA Enterprise

## Usage
Run `WifiPasswordExtractorGUI.exe`

or use CUI tool
```
> WifiPasswordExtractor.exe
System may prompt UAC 2 times in scan.
Scanning..................
========== RESULT ==========
enterprisewifi: user : pass
enterprisewifi2: user@domain : pass
personalwifi: pass
openwifi
```

## How it works.
This Program uses some technology to extract passwords and ids.

### Normal Wi-Fi Credential (Personal)
Get all xml files from `C:\ProgramData\Microsoft\Wlansvc\Profiles\Interfaces` and parse these xmls.

If credential is encrypted, Program uses 2 ways to decrypt it.
1. Use `System.Security.Cryptography.ProtectedData.Unprotect` method with `SYSTEM` user.
2. Run `netsh.exe wlan export profile name="..." key=clear folder=..."`
> In default, Program will use 1st way.

### Enterprise Wi-Fi Credential
Program try to use following steps to get data.
1. Get encrypted raw profile data from registry.
2. Use `System.Security.Cryptography.ProtectedData.Unprotect` method with `SYSTEM` user to decrypt raw profiles.
3. Search User/Domain string from decrypted raw profile.
4. Search Password.
5. If Password has been encrypted, use `System.Security.Cryptography.ProtectedData.Unprotect` method with local user to decrypt it.

#### How to find User, Domain and Password from decrypted raw profile
> method: `WifiPasswordExtractProxy.DataExtractor.TryExtractDomainAndUserFromExtractedData`, `WifiPasswordExtractProxy.DataExtractor.TryExtractPasswordFromExtractedData`
> 
> source: `/WifiPasswordExtractProxy/DataExtractor.cs`
---

##### Type 1
`0x01, 0x00, 0x00, 0x00, 0xD0, 0x8C, 0x9D, 0xDF, 0x01`'s next byte is start of username. Username ends before `0x00`.

The byte not `0x00` after username is Domain's field (if byte is `0xE6` or not found, Domain is unavailable). Domain information ends before next `0x00`.

The encrypted password field starts with `0x01, 0x00, 0x00, 0x00, 0xD0, 0x8C, 0x9D, 0xDF, 0x01` and ends before `0x00`.

Encoding is ASCII.

##### Type 2
`0x04, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00`'s next byte is start of username. Username ends before `0x00`.

The byte not `0x00` after username stores password. Password ends before next `0x00`.

The byte not `0x00` after password is Domain's field (if byte is `0xE6` or not found, Domain is unavailable). Domain information ends before next `0x00`.

Encoding is ASCII.

### SYSTEM User Elevation
> method: `WifiPasswordExtractProxy.Program.RunAsSystem`, `WifiPasswordDecryptProxy.Program.RunAsSystem`
> 
> source: `/WifiPasswordExtractProxy/Program.cs`, `/WifiPasswordDecryptProxy/Program.cs`
---

This program uses `schtasks.exe` (Task Scheduler's CUI controller) to run program in SYSTEM user.

> **Notice**: This method only works with `Administrator` privilege.

1. Run `schtasks.exe /create /f /sc Once /tn "<TASK NAME>" /tr "<Executable>" /st 23:59 /ru "SYSTEM" /V1 /Z` to create Task to run program with `SYSTEM` user.

|Option|Role|
|:--|:--|
|/create|create new task|
|/f|force (overwrite)|
|/sc `Once`|Set schedule type: `Once`|
|/tn "`<TASK NAME>`"|Set task name: `<TASK NAME>`|
|/tr "`<Executable>`"|Set executable: `<Executable>`|
|/st `23:59`|Run in `23:59` (This is necessary option, but not used)|
|/ru "`SYSTEM`"|Run with user `SYSTEM`|
|/V1|(To use `/Z` option)|
|/Z|Delete when finished|

2. Run `schtasks.exe /run /tn "<TASK NAME>"` to run task made in `1.`.
Application specificated in `1.` run with `SYSTEM` user in this step.

|Option|Role|
|:--|:--|
|/run|run task|
|/tn "`<TASK NAME>`"|Task name: `<TASK NAME>`|

3. Run `schtasks.exe /delete /f /tn "<TASK NAME>"` to delete task.

|Option|Role|
|:--|:--|
|/delete|delete task|
|/f|force (no prompt)|
|/tn "`<TASK NAME>`"|Task name: `<TASK NAME>`|


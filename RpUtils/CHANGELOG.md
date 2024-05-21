# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

### Changed
    - RP Now tab now rolls up maps with sublocations like Steps of Thal and Eulmore.
	- Added 'open map' button to map locations in the current server of the user.
### Removed

## [0.2.2]

### Changed
	- Roleplaying location will now be removed if the user logs out or in a PVP mode outside of Wolve's Den.
	- Added checks against TerritoryIntendedUse to prevent reporting locations from places we don't want
	- Added sub region names to Rp now tab

## [0.2.1]

### Added
	- Notifications for when player is and isn't sharing location with server

### Changed
	- Updated label for the DtrEntry
	- Updated RP Now table to include a collapsable / sortable tree by server instead of a flat list.

### Removed

## [0.2.0]

### Added
	- Added SettingsTab which shows general plugin settings
	- Added SonarConfigTab which shows RP Sonar settings
	- Added CurrentRpTab which shows the counts of RPers in world/zones, as well as the current listeners

### Changed
	- Rewrote existing UI to make use of WindowSystem, and split into tabs
	- Extracted DtrEntry from SonarController into a service, now opens to the RpUtils window

## [v0.1.0]

### Added
	- DtrEntry for toggling RP Sonar and status
	- Config option for showing or hiding the DtrEntry

### Changed
	- No longer report locations if in housing districts

## [v0.0.4]

### Changed
	- Fixed markers not showing or showing with odd placements due to AddMapMarker incorrectly applying scaling for the current map to raw positions for selected maps

### Removed
	- Removed custom repo json file, as it is no longer needed	

## [v0.0.3]

### Added
	- Added changelog (You are reading it!)

### Changed
	- Updated to API version 0.0.2
	- Fixed double calls to enabling SonarController, causing double listeners for opening the map

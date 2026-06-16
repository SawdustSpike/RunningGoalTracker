# StrideIQ

A modern running analytics dashboard built with Blazor and .NET 9 that helps runners track annual mileage goals, compare actual progress against planned targets, and forecast year-end performance using real Strava activity data.

## Features

### Goal Tracking

* Annual mileage goal management
* Miles vs kilometers support
* Manual mileage adjustments
* Progress visualization

### Strava Integration

* OAuth 2.0 authentication
* Automatic token refresh
* Year-to-date mileage synchronization
* Monthly activity aggregation

### Analytics

* Projected year-end mileage
* Projected finish date
* Ahead/behind pace calculations
* Monthly target breakdown
* Actual vs expected monthly performance
* Goal scenario planning

### User Experience

* Responsive dashboard design
* Light and dark themes
* Local persistence
* Advanced configuration options

## Tech Stack

* .NET 9
* Blazor Server
* C#
* Strava API
* Local Storage
* OAuth 2.0

## Architecture

The application is built using a service-oriented architecture:

### Services

* GoalProgressService
* StravaService
* StravaApiService
* StravaAuthService
* LocalStorageService

### Components

* GoalSetupPanel
* MonthlyPlanTable
* MonthlyProgressChart
* GoalScenarioPanel
* MonthlyAllocationEditor

---
## Screenshots

## Dashboard (Dark Mode)

![Dark Mode Dashboard](docs/images/Dark%20Mode%20Dash.png)

---

## Dashboard (Light Mode)

![Light Mode Dashboard](docs/images/Light%20Mode%20Dash.png)

---

## Monthly Analytics

![Monthly Analytics](docs/images/Monthly%20Analytics.png)

---

## Stretch Goal Planning

![Stretch Goals](docs/images/Stretch%20Goals.png)

---

## Architecture

### Components

![Components](docs/images/Components.png)

### Goal Progress Service

![Goal Progress Service](docs/images/Goal%20Progress%20Service.png)

### Strava Service

![Strava Service](docs/images/StravaService.png)

---

## Future Enhancements

* Regional training templates
* AI-powered goal recommendations
* MAUI desktop application
* Historical year-over-year comparisons
* Achievement system expansion

## What I Learned

This project provided hands-on experience with:

* OAuth authentication flows
* Third-party API integration
* Dependency injection
* Blazor component architecture
* Data visualization
* State persistence
* Responsive UI design
* Forecasting and analytics logic

## Author

Michael Cowell

Senior Software Engineer

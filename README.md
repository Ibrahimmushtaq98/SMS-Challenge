# SMS Gatekeeper (Challenge)

SMS Gatekeeper is a .NET Core microservice designed to manage SMS message rate limits for businesses. It acts as a gatekeeper, ensuring messages respect provider-imposed limits (per phone number and account-wide) before calling an external messaging API, thus avoiding unnecessary costs.

## Project Structure
* `SMS-Gatekeeper`: The Main microservice project
* `SMS-Gatekeeper-Tests`: Unit test for the ratelimiter
* `SMS-Gatekeeper-Core`: Contains the core code for the microservice

## Prerequisites
* .NET 8 SDK
* Visual Studio 2022

### How to run the project
Have the project cloned on to your machine. Open up the solution file and select `SMS-Gatekeeper` as the main project to run

To run the testing project, simply right click on the `SMS-Gatekeeper-Test` and select run test

## Configuration
- Settings: Configurable in `SMS-Gatekeeper/appsettings.json`:
    - `MaximumMessagePerBuisnessNumberPerSecond`: Max messages per phone number per second.
    - `MaxMessagesPerAccountPerSecond`: Max messages across the account per second.
    - `InactivityTimeoutSecond`: Time before inactive numbers are cleaned up in seconds.
    - `CleanupIntervalSecond`: Frequency of cleanup checks in seconds.

- **Example**
```json
  "RateLimiterOptions": {
    "MaximumMessagePerAccountPerSecond": 3,
    "MaximumMessagePerBuisnessNumberPerSecond": 3,
    "InactivityTimeoutSecond": 60,
    "CleanupIntervalSecond":  120
  }
```

## Potential Improvements 
-   **Performance**: Optimize concurrency (e.g., reduce locks) for higher throughput.
  
-   **Production Readiness**: Add ways to gracefully shutdown, and possibly health checks.
  
-   **Logging**: Better logging for much clearer information.
  
-   **Testing**: Expand test cases and add integration tests for end-to-end validation.
  
-   **Scalability**: Implement distributed rate limiting (e.g., with Redis) for multi-instance deployments.
  
-   **Usability**: Improve API responses with detailed info and enhance Swagger docs.
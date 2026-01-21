# 3Ugers Industrial Programming

Avalonia desktop app that lets operators manage customers and orders by a UR robot arm to sort colour-coded blocks for each customer. Orders are stored in SQLite, secured by salted+hashed credentials, and dispatched to the robot as URScript jobs.

## Important Notes
- **Remove the seeded `admin` user and default password before deploying to production.**
- **Always retrain the robot camera when introducing new blocks / products.**

## Key Features
- Login/registration with salted PBKDF2 password hashes and basic lockout/inactivity timers
- Customer and order CRUD backed by Entity Framework Core + SQLite
- Robot dashboard for connecting/disconnecting and dispatching generated URScripts
- Example proposal + flow documentation for onboarding new teammates

## GUI Flow
<img width="953" height="394" alt="image" src="https://github.com/user-attachments/assets/a8ebd5b9-5a32-42f4-9bb4-055143ec9bb3" />


## Sorting Flow
<img width="960" height="370" alt="image" src="https://github.com/user-attachments/assets/1f1b3877-431f-47a3-b6e8-648f8eb10ee8" />




# 3Ugers Industrial Programming

Avalonia desktop app that lets operators manage customers and orders while driving a UR robot arm to sort colour-coded blocks for each customer. Orders are stored in SQLite, secured by salted+hashed credentials, and dispatched to the robot as URScript jobs.

## Important Notes
- **Remove the seeded `admin` user and default password before deploying to production.**
- **Always retrain the robot camera when introducing new block colours or products.**

## Key Features
- Login/registration with salted PBKDF2 password hashes and basic lockout/inactivity timers
- Customer and order CRUD backed by Entity Framework Core + SQLite
- Robot dashboard for connecting/disconnecting and dispatching generated URScripts
- Example proposal + flow documentation for onboarding new teammates

## GUI Flow
```mermaid
flowchart LR
    start([Start]) --> login[Log in]
    login --> registered{Registered?}
    registered -- No --> login
    registered -- Yes --> admin{Admin?}
    admin -- Yes --> adminDash[Admin Dashboard]
    admin -- No --> userDash[User Dashboard]

    subgraph Admin Actions
        adminDash --> connectA[Connect Robot]
        adminDash --> sortA[Sort Order]
        adminDash --> createUser[Create User]
        adminDash --> createCust[Create Customer]
        adminDash --> createOrderA[Create Order]
    end

    subgraph User Actions
        userDash --> connectU[Connect Robot]
        userDash --> sortU[Sort Order]
        userDash --> createOrderU[Create Order]
        userDash --> readCust[Read Customer]
    end

    adminDash --> afk{AFK > 5 min?}
    userDash --> afk
    afk -- Yes --> logout[Log out]
    afk -- No --> adminDash
    logout --> end([End])
```

## Sorting Flow
```mermaid
flowchart LR
    start([Start]) --> moveCamera[Move to camera view]
    moveCamera --> find{Brick seen within 15s?}
    find -- No --> end([End])
    find -- Yes --> savePose[Save workpiece pose / Get workpiece]
    savePose --> approach[Approach workpiece]
    approach --> pick[Pick workpiece]
    pick --> wayOrder{Workpiece matches order?}
    wayOrder -- Yes --> orderWaypoint[Move to order waypoint]
    wayOrder -- No --> resortWaypoint[Move to resort waypoint]
    orderWaypoint --> moveCamera
    resortWaypoint --> moveCamera
```

## Proposal & Documentation
The `Projektproposal.pdf` file in the repository is the original Danish project description submitted for the course and can be shared alongside the README for stakeholders.

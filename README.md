# VOX Interaction Model Design

## Overview



## Musical Design Philosophy

The experience uses voice input to create a novel form of musical interaction where spatial position directly maps to musical pitch. This creates an intuitive relationship between the player's voice, their movement in the virtual space, and musical progression.

### Core Musical-Spatial Mapping

The system maps musical frequencies to physical heights using logarithmic scaling:
- Lower frequencies (deeper notes) are positioned closer to the ground
- Higher frequencies (higher notes) are positioned higher in the space
- Height mapping follows standard musical intervals (logarithmic scaling)
- Base height (4m) corresponds to approximately 100Hz
- Maximum height (20m) corresponds to notes above C4 (middle C)

### Musical Sequence Design

The experience guides players through a meditative C minor progression:

```
C3  (130.81 Hz) → Base position
Eb3 (155.56 Hz) → Slightly higher
G3  (196.00 Hz) → Middle height
C4  (261.63 Hz) → Peak height
G3  (196.00 Hz) → Return to middle
Eb3 (155.56 Hz) → Lower again
C3  (130.81 Hz) → Return to base
```

This sequence was chosen to:
- Start with comfortable, achievable notes
- Create a natural arc of rising and falling
- Use musically related intervals
- Return to the starting note for closure

## Player Interaction

### Voice-Height Mapping

Players control their height through voice pitch:
1. Voice frequency is analyzed in real-time using the McLeod Pitch Method
2. Detected frequency is mapped to a target height using logarithmic scaling
3. Player's vertical position smoothly transitions to match their voice pitch
4. Visual feedback shows current and target heights
5. Confidence metrics ensure stable pitch detection

### Musical Pickup Collection

Musical pickups are placed at heights corresponding to their frequencies:
1. Each pickup represents a note in the sequence
2. Pickups are positioned ahead of the player in a path
3. Players must match the pickup's pitch to reach its height
4. Visual and audio feedback indicate proximity to correct pitch
5. Successful collection requires maintaining pitch for a brief duration

### Height Calculation

```
Height = baseHeight + (log(frequency) - log(minFreq)) / (log(maxFreq) - log(minFreq)) * (maxHeight - baseHeight)

Where:
- baseHeight = 4 meters (height at 100 Hz)
- maxHeight = 20 meters (height at maximum frequency)
- minFreq = 80 Hz (lowest supported note)
- maxFreq = 300 Hz (highest supported note)
```

## Visual Feedback

### Pickup Visualization
- Pickup color indicates sequence position
- Opacity changes based on current/target status
- Size pulses subtly to draw attention
- Forward path shows upcoming sequence
- Height lines show pitch targets

### Player Feedback
- Current pitch shown as floating sphere
- Color gradient indicates pitch accuracy
- Particle effects for successful matches
- Distance markers show height targets
- Frequency and confidence displays

## Implementation Details

### Key Components

1. **MPMAudioAnalyzer**
   - Real-time pitch detection
   - Frequency confidence calculation
   - Amplitude envelope following

2. **PlayerMovementController**
   - Voice-to-height mapping
   - Smooth height transitions
   - Ground tracking
   - Collision detection

3. **PickupManager**
   - Musical sequence management
   - Pickup spawning and positioning
   - Collection detection
   - Progress tracking

### Configuration Parameters

```csharp
[Header("Height Mapping")]
float baseHeight = 4f;     // Height at 100 Hz
float maxHeight = 20f;     // Maximum height
float minFreq = 80f;       // Lowest note (below C3)
float maxFreq = 300f;      // Highest note (above C4)

[Header("Musical Sequence")]
MusicalPickup[] sequence = {
    new MusicalPickup { frequency = 130.81f }, // C3
    new MusicalPickup { frequency = 155.56f }, // Eb3
    new MusicalPickup { frequency = 196.00f }, // G3
    new MusicalPickup { frequency = 261.63f }, // C4
    new MusicalPickup { frequency = 196.00f }, // G3
    new MusicalPickup { frequency = 155.56f }, // Eb3
    new MusicalPickup { frequency = 130.81f }  // C3
};
```


### Player Control Details

Two inputs control the character movement.

1. Audio of the user's voice  
    a. Sensor: device microphone  
    b. Controls: amplitude and frequency
    c. Mappings:  
    1. Amplitude drives elevation on the y axis with an envelope follower, feature a fairly rapid attack and very slow logarithmic release </i>
    2. Frequency modulates the y axis elevation within the "pitch fly zone", with a more responsive envelope.
2. Tilt of the headset 
    a. Sensor: gyroscope  
    b. Controls: azimuth plane  
    c. Mappings: forward, backward, left, right moves character slowly in that direction, 360 degrees on the azimuth plane </i>


## Scene Hierarchy and Components

```
UnityAudioProto2
├── AudioManager
│   └── MPMAudioAnalyzer (Pitch detection and voice processing)
├── [BuildingBlock] Camera Rig
│   ├── Player (Movement controller)
│   └── TrackingSpace
│       ├── CenterEyeAnchor
│       │   └── PitchVisualizer
│       ├── LeftEyeAnchor
│       └── RightEyeAnchor
├── TerrainManager (Procedural terrain generation)
├── LandmarkManager (Dynamic reference objects)
├── ReferenceGrid
├──PickupManager (Musical progression system) [In progress]
│   ├── PickupManager 
```
### Core Systems

1. **Voice Analysis (MPMAudioAnalyzer)**
   - Real-time pitch detection using McLeod Pitch Method
   - Amplitude envelope following
   - Frequency and clarity analysis

2. **Movement System (PlayerMovementController)**
   - Voice-driven elevation control
   - Pitch-based height modulation
   - Head-tilt based directional control
   - ADSR envelope for smooth transitions

3. **Terrain Generation (TerrainManager)**
   - Procedural mesh generation
   - Dynamic chunk loading
   - Collision detection
   - Performance-optimized updates

4. **Landmark System (LandmarkManager)**
   - Dynamic spawn system
   - Distance-based visual feedback
   - Cardinal direction markers
   - Spatial reference points

5. **Musical Progression (PickupManager)**
   - C minor meditation sequence
   - Visual waypoint system
   - Audio feedback (each pickup emits the target note)
   - Progress tracking (unclear how this is to be implemented)



## Setup Requirements

### Hardware
- Meta Quest or similar VR headset
- Microphone input
- Stereo audio output

### Software Dependencies
- Unity 2022.3 or later
- Meta XR SDK
- Universal Render Pipeline (URP)
- TextMeshPro



## System Architecture

```mermaid
graph TD
    subgraph Audio Analysis
        MA[Microphone Audio] --> MPM[MPM Analyzer]
        MPM --> FA[Frequency Analysis]
        MPM --> AA[Amplitude Analysis]
        FA --> PC[Pitch Confidence]
        FA --> FD[Frequency Detection]
        AA --> EF[Envelope Follower]
    end

    subgraph Visual Feedback
        FD --> PS[Pitch Spheres]
        PC --> VC[Visual Confidence]
        PS --> CM[Color Mapping]
        PS --> PA[Pitch Accuracy]
        PA --> PFX[Particle Effects]
    end

    subgraph Movement System
        EF --> VM[Vertical Movement]
        FD --> PM[Pitch Modulation]
        VM --> PH[Physics Handler]
        PM --> PH
    end

    subgraph Gameplay
        PH --> PC[Player Controller]
        PC --> PP[Pickup Processing]
        PP --> MS[Musical Sequence]
        MS --> GM[Game Manager]
    end
```


## Core Systems Detail

### MPM Audio Analysis System

```mermaid
flowchart TB
    subgraph Audio Pipeline
        direction TB
        A[Audio Input] --> B[Buffer]
        B --> C[NSDF Calculation]
        C --> D[Peak Detection]
        D --> E[Frequency Estimation]
        E --> F[Confidence Check]
    end

    subgraph Debug Visualization
        direction TB
        G[Buffer View] --> H[NSDF Plot]
        H --> I[Peak Markers]
        I --> J[Confidence Display]
    end

    F --> G
```

### Visual Feedback System

```mermaid
stateDiagram-v2
    [*] --> OutOfRange
    OutOfRange --> NearTarget: Within 50 cents
    NearTarget --> CloseToTarget: Within 25 cents
    CloseToTarget --> OnTarget: Within 5 cents
    
    OnTarget --> CloseToTarget: Drift > 5 cents
    CloseToTarget --> NearTarget: Drift > 25 cents
    NearTarget --> OutOfRange: Drift > 50 cents

    state OutOfRange {
        [*] --> Blue
    }
    state NearTarget {
        [*] --> Orange
    }
    state CloseToTarget {
        [*] --> Yellow
    }
    state OnTarget {
        [*] --> Green
    }
```

## C Minor Meditation Sequence

```mermaid
graph LR
    Start --> C3
    C3 --> Eb3
    Eb3 --> G3
    G3 --> C4
    C4 --> G3_2[G3]
    G3_2 --> Eb3_2[Eb3]
    Eb3_2 --> C3_End[C3]
    C3_End --> End
```

## Implementation Priorities

1. **Phase 1: Audio Analysis Foundation**
   - MPM implementation debugging
   - Real-time visualization tools
   - Frequency detection validation
   - Amplitude envelope system

2. **Phase 2: Visual Feedback System**
   - Pitch sphere behavior
   - Color transition system
   - Accuracy visualization
   - Musical note display

3. **Phase 3: Movement System**
   - Amplitude-based hovering
   - Pitch-based modulation
   - Physics integration
   - Movement bounds

4. **Phase 4: Gameplay Elements**
   - Pickup system
   - Musical sequence implementation
   - Progress tracking
   - Success feedback

## Testing Strategy

```mermaid
graph TD
    subgraph Unit Tests
        UT1[MPM Algorithm] --> UT2[Frequency Detection]
        UT2 --> UT3[Amplitude Analysis]
    end

    subgraph Integration Tests
        IT1[Audio-Visual Sync] --> IT2[Movement Response]
        IT2 --> IT3[Pickup Interaction]
    end

    subgraph User Testing
        US1[Pitch Feedback] --> US2[Movement Feel]
        US2 --> US3[Musical Flow]
    end

    UT3 --> IT1
    IT3 --> US1
```

## Debug Visualization Tools

- NSDF Buffer View
- Frequency Spectrum Display
- Pitch Confidence Meter
- Amplitude Envelope Monitor
- Physics Debug View

## Visual Feedback Elements

1. **Pitch Accuracy Indicators**
   - Expanding/contracting aura
   - Particle system intensity
   - Color transitions
   - Distance lines

2. **Musical Information**
   - Current note name
   - Target note
   - Cents deviation
   - Octave indicator

3. **Performance Feedback**
   - Pitch history trail
   - Success particles
   - Collection effects
   - Level progress

## Implementation Phases

```mermaid
gantt
    title Development Timeline
    dateFormat  YYYY-MM-DD
    section Audio Analysis
    MPM Debug & Fix            :a1, 2025-01-05, 2d
    Frequency Detection        :a2, 2025-01-05, 2d
    Amplitude Analysis         :a3, 2025-01-05, 2d

    section Visual Feedback
    Basic Pitch Display        :v4, 2025-01-05, 2d
    Color System              :v5, 2025-01-05, 2d
    Accuracy Visualization    :v6, 2025-01-05, 2d

    section Movement
    Envelope Follower         :m1, after a3, 1d
    Physics Integration       :m2, after m1, 1d
    Movement Controls         :m3, after m1, 1d

    section Gameplay
    Pickup System             :g1, after m1, 1d
    Musical Sequence          :g2, after g1, 1d
    Polish & Testing          :g3, after g2, 1d
```

# Voice Driven World Interaction


## Scene Hierarchy and Components

```
UnityAudioProto*
├── AudioManager
│   ├── Audio Source
│   └── MPM Audio Analyzer
├── Directional Light
└── [BuildingBlock] Camera Rig
    └── TrackingSpace
        └── CenterEyeAnchor
            └── PitchVisualizer
                ├── CurrentPitchSphere
                │   └── Sphere
                ├── TargetPitchSphere
                │   └── Sphere
                └── Labels
                    ├── MinFrequencyLabel
                    ├── MaxFrequencyLabel
                    ├── FrequencyLabel
                    └── ConfidenceLabel
```

### Component Descriptions

#### MPM Audio Analyzer
Primary component for pitch detection and audio analysis. Configurable parameters:
- **Analysis Configuration**
  - Buffer Size: 2048 samples
  - Clarity Threshold: 0.71
  - Noise Floor: 0.001
  - Use Key Frequencies: true/false

- **Voice Range**
  - Min Frequency: 100 Hz
  - Max Frequency: 600 Hz

- **Debug Options**
  - Debug Mode: Enable detailed logging
  - Show Detailed Debug: Show extended debug info
  - Log Pitch Data: Log frequency and confidence
  - Visualize Buffers: Show buffer data

Dependencies:
- Requires AudioSource component
- Outputs: Frequency, Confidence, Clarity, Amplitude

#### PitchVisualizer
Visual feedback system for pitch matching. Parameters:
- **Layout**
  - Visualizer Distance: 2m from camera
  - Max Vertical Angle: 20 degrees
  - Vertical Offset: -0.2m
  - Sphere Base Scale: 0.05

- **Visualization Settings**
  - Target Frequency: 130.81 Hz (C3)
  - Frequency Tolerance: 5 Hz

- **Colors**
  - Target Color: Green (0, 1, 0, 0.8)
  - Normal Color: Blue (0, 0.5, 1, 0.8)
  - Close Color: Yellow (1, 1, 0, 0.8)
  - Matched Color: Red (1, 0, 0, 1)

- **Text Customization**
  - Font Size: 10
  - Label Offset: 0.15
  - Label Size: 0.02

Dependencies:
- Requires MPMAudioAnalyzer reference
- Requires TextMeshPro for labels
- Requires sphere primitives for visual feedback

### Installation Requirements

1. Unity Packages:
   - TextMeshPro
   - Universal Render Pipeline (URP)
   - XR Interaction Toolkit
   - Meta XR SDK

2. Scene Setup:
   ```
   1. Create base scene structure
   2. Add AudioManager with MPMAudioAnalyzer
   3. Add PitchVisualizer under CenterEyeAnchor
   4. Create sphere primitives
   5. Configure materials and shaders
   6. Set up text components
   ```

3. Material Requirements:
   - URP/Lit shader for spheres
   - Transparent materials for feedback
   - TextMeshPro font assets

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
    Movement Controls         :m3, after m2, 1d

    section Gameplay
    Pickup System             :g1, after m3, 1d
    Musical Sequence          :g2, after g1, 1d
    Polish & Testing          :g3, after g2, 1d
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

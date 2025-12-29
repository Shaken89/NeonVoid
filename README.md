# 🚀 NeonVoid - Space Arcade Shooter

<div align="center">

![NeonVoid Banner](https://img.shields.io/badge/Unity-2022.3+-black?style=for-the-badge&logo=unity)
![Platform](https://img.shields.io/badge/Platform-PC%20%7C%20Mobile-blue?style=for-the-badge)
![Genre](https://img.shields.io/badge/Genre-Arcade%20Shooter-purple?style=for-the-badge)

**An intense top-down space shooter with progression, upgrades, and endless waves**

[Features](#-features) • [Gameplay](#-gameplay) • [Controls](#-controls) • [Team](#-team) • [Installation](#-installation)

</div>

---

## 🎮 Game Concept

**NeonVoid** is a fast-paced arcade space shooter where you pilot a lone spacecraft through endless waves of enemies in the void of space. Master your piloting skills, upgrade your ship, and survive as long as possible!

### Genre & Mechanics

- **Genre:** Top-down Arcade Shooter with Roguelike elements
- **Core Loop:** Survive waves → Level up → Choose upgrades → Face harder enemies
- **Art Style:** Neon-styled 2D sprites with particle effects
- **Platform:** PC (Windows) and Mobile (Android/iOS ready)

---

## ✨ Features

### 🎯 Core Gameplay
- **Smooth movement and responsive controls** using Unity's New Input System
- **Mouse-aimed shooting** with automatic fire option
- **Multiple enemy types** with unique behaviors:
  - Basic Chasers - fast but weak
  - Swarm enemies - attack in groups
  - Berserkers - aggressive melee attackers
  - Necromancers - summon minions
  - Bosses - challenging encounters with health bars

### 📊 Progression System
- **Level-up system** with experience gained from kills
- **Modular upgrade system** with choices after each level:
  - Damage upgrades
  - Fire rate boosts
  - Speed increases
  - Shield regeneration
  - Special abilities (Dash, Multi-shot)
- **Wave-based difficulty scaling** - enemies get stronger over time
- **Score tracking** with high score persistence

### 💾 Save System
- **PlayerPrefs-based saves** for settings and progress
- **High score persistence** between sessions
- **Settings saved automatically**:
  - Audio levels (Music, SFX)
  - Graphics quality
  - Control sensitivity

### 🎨 Visual & Audio
- **Consistent neon visual style** with particle effects
- **Dynamic UI** with health/shield bars, ammo counter, score display
- **Sound effects** for shooting, explosions, and UI interactions
- **Background music** (ready to integrate)
- **Object pooling** for performance optimization

### 🎮 User Interface
- **Main Menu** with Play, Settings, Quit options
- **Pause Menu** during gameplay
- **Settings Menu** with volume sliders and graphics options
- **Game Over Screen** with restart and main menu options
- **HUD** displaying:
  - Health and Shield bars
  - Score and Wave number
  - Ammo counter
  - Level and XP progress bar
  - Active upgrade indicators

### 🏆 Gameplay Features
- **Screen wrapping** - fly off one edge, appear on the opposite
- **Power-ups** spawning system
- **Boss encounters** every 5 waves
- **Combo system** for consecutive kills
- **Different difficulty levels**

---

## 🎯 Gameplay

1. **Start the game** from the Main Menu
2. **Survive waves** of enemies and shoot them down
3. **Gain experience** from each kill
4. **Level up** and choose powerful upgrades
5. **Face increasingly difficult waves** with new enemy types
6. **Defeat bosses** for big rewards
7. **Try to beat your high score!**

---

## 🕹️ Controls

### Keyboard & Mouse (PC)
- **WASD** or **Arrow Keys** - Move ship
- **Mouse** - Aim direction
- **Left Mouse Button** or **Space** - Shoot
- **ESC** - Pause game
- **Shift** - Dash (when unlocked)

### Touch (Mobile)
- **Left side touch** - Virtual joystick for movement
- **Right side touch** - Aim and shoot
- **Pause button** - Top-right corner

---

## 👥 Team

### Developers
- **Lead Developer & Programmer** - [Your Name]
- **Game Designer** - [Your Name]
- **UI/UX Designer** - [Your Name]

### Assets Used
- **Tiny Ships** - Ship sprites by [Asset Pack Author]
- **Essential 2D Particle FX** - Particle effects
- **Casual Game Sounds** - Sound effects library
- **Gabriel Aguiar Productions** - Additional assets

### Technologies
- **Unity 2022.3+** - Game Engine
- **C#** - Programming Language
- **Unity Input System** - New input handling
- **Universal Render Pipeline (URP)** - Rendering
- **TextMesh Pro** - UI Text

---

## 🏗️ Project Structure

```
NeonVoid/
├── Assets/
│   ├── Scenes/
│   │   ├── MainMenu.unity          # Main menu scene
│   │   └── SampleScene.unity       # Main gameplay scene
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── PlayerController.cs
│   │   │   ├── PlayerHealth.cs
│   │   │   └── WeaponManager.cs
│   │   ├── Enemies/
│   │   │   ├── Enemy.cs
│   │   │   ├── SwarmEnemy.cs
│   │   │   ├── BerserkerEnemy.cs
│   │   │   ├── NecromancerEnemy.cs
│   │   │   └── BossEnemy.cs
│   │   ├── Systems/
│   │   │   ├── ExperienceSystem.cs
│   │   │   ├── ModularUpgradeSystem.cs
│   │   │   ├── SaveManager.cs
│   │   │   └── EnemySpawner.cs
│   │   ├── UI/
│   │   │   ├── MainMenu.cs
│   │   │   ├── PauseMenu.cs
│   │   │   ├── HUDController.cs
│   │   │   └── GameOverScreen.cs
│   │   └── Managers/
│   │       ├── AudioManager.cs
│   │       ├── ObjectPoolManager.cs
│   │       └── ParticleManager.cs
│   ├── Prefabs/
│   │   ├── Player1.prefab
│   │   ├── Bullet.prefab
│   │   ├── EnemyBullet.prefab
│   │   └── VFX/
│   └── Settings/
└── ProjectSettings/
```

---

## 🚀 Installation

### For Development
1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/NeonVoid.git
   ```
2. Open the project in **Unity 2022.3 or later**
3. Wait for Unity to import all assets
4. Open `Scenes/MainMenu.unity`
5. Press **Play**

### For Players (Build)
1. Download the latest release from [Releases](https://github.com/yourusername/NeonVoid/releases)
2. Extract the ZIP file
3. Run `NeonVoid.exe` (Windows) or install the APK (Android)

---

## 🎮 Building the Game

### PC Build (Windows)
1. Go to **File → Build Settings**
2. Select **PC, Mac & Linux Standalone**
3. Architecture: **x86_64**
4. Click **Build**

### Mobile Build (Android)
1. Go to **File → Build Settings**
2. Switch platform to **Android**
3. Configure **Player Settings**:
   - Package Name
   - Minimum API Level: 23
   - Target API Level: 33+
4. Click **Build**

---

## 📊 Technical Highlights

### Code Quality
- ✅ **Consistent coding style** with proper naming conventions
- ✅ **Comprehensive XML documentation** for all public methods
- ✅ **Modular architecture** with separated concerns
- ✅ **Event-driven systems** for decoupled components
- ✅ **Object pooling** for performance optimization
- ✅ **SOLID principles** applied throughout

### Performance Optimizations
- Object pooling for bullets and enemies
- Efficient particle system management
- Optimized collision detection
- Proper use of FixedUpdate for physics
- Minimal garbage collection through object reuse

### Advanced Features
- New Unity Input System integration
- Modular upgrade system with data-driven design
- Save/Load system with JSON serialization
- Dynamic difficulty scaling
- Responsive UI with Canvas Scaler
- Screen size adaptation for multiple resolutions

---

## 🎯 Milestones & Achievements

### ✅ Milestone 1 - Foundation (Completed)
- [x] GitHub repository setup
- [x] Core gameplay mechanics
- [x] Player controller with shooting
- [x] Basic enemy AI

### ✅ Milestone 2 - Core Systems (Completed)
- [x] Enemy variety (4+ types)
- [x] Experience and level system
- [x] Upgrade system
- [x] Save/Load functionality

### ✅ Milestone 3 - Polish (Completed)
- [x] Complete UI (Main Menu, HUD, Pause, Game Over)
- [x] Settings menu with persistence
- [x] Audio system
- [x] Visual effects and particles
- [x] Boss fights

### 🎉 Bonus Features
- Advanced enemy behaviors (Swarm, Berserker, Necromancer)
- Modular upgrade system
- Object pooling
- Screen wrapping
- Dash ability
- Boss health bars

---

## 📝 Credits

### Open Source Assets
- Unity Technologies - Unity Engine
- Unity Input System Package
- TextMesh Pro Package
- Universal Render Pipeline

### Community
Thanks to the Unity community and asset creators for making game development accessible!

---

## 📄 License

This project is created for educational purposes.

---

## 🐛 Known Issues & Future Plans

### Known Issues
- None currently! Please report bugs via GitHub Issues

### Planned Features
- [ ] More enemy types
- [ ] Additional weapons
- [ ] Endless mode leaderboard
- [ ] Achievement system
- [ ] Multiple ship types to choose from
- [ ] Power-up variety expansion

---

<div align="center">

**Made with ❤️ using Unity**

[⬆ Back to Top](#-neonvoid---space-arcade-shooter)

</div>

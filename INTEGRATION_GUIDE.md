# 🎮 Полный Гайд по Внедрению Скриптов NeonVoid

## 📋 Содержание
1. [Этап 1: Базовые Системы](#этап-1-базовые-системы)
2. [Этап 2: UI и Сохранения](#этап-2-ui-и-сохранения)
3. [Этап 3: Враги и Контент](#этап-3-враги-и-контент)
4. [Настройка Input System](#настройка-input-system)
5. [Создание Prefab'ов](#создание-prefabов)
6. [Настройка Сцен](#настройка-сцен)

---

## Этап 1: Базовые Системы

### 1. AudioManager
**Файл:** `AudioManager.cs`

#### Настройка в Unity:
1. Создайте пустой GameObject: `GameObject → Create Empty`
2. Назовите его **"AudioManager"**
3. Добавьте компонент: `Add Component → AudioManager`
4. Настройте в Inspector:
   - **Music Source** - перетащите AudioSource для музыки
   - **SFX Source** - перетащите AudioSource для звуков
   - **SFX Pool Size** - 10 (количество одновременных звуков)

#### Добавление музыки:
1. Импортируйте ваши .mp3/.wav файлы в папку `Assets/Audio/Music/`
2. В другом скрипте вызовите:
```csharp
AudioManager.Instance.PlayMusic(musicClip, true, 1f);
```

#### Добавление звуков:
```csharp
AudioManager.Instance.PlaySFX(shootSound, 0.5f);
```

---

### 2. PlayerController
**Файл:** `PlayerController.cs`

#### Настройка:
1. Выберите объект игрока (обычно с тегом "Player")
2. Добавьте компоненты:
   - `Rigidbody2D` (если нет)
     - Body Type: Dynamic
     - Gravity Scale: 0
     - Collision Detection: Continuous
     - Interpolate: Interpolate
   - `Add Component → PlayerController`

3. Настройте в Inspector:

**Movement Settings:**
- Move Speed: 5
- Acceleration: 0.2
- Use Boundaries: ✓
- Screen Padding: 0.5

**Shooting Settings:**
- Bullet Prefab: (перетащите prefab пули)
- Fire Point: (создайте пустой Transform на носу корабля)
- Bullet Speed: 15
- Fire Rate: 0.2
- Max Ammo: 100
- Auto Reload: ✓
- Reload Time: 2

**Visual Settings:**
- Aim Sprite: (ваш спрайт корабля)
- Smooth Rotation: ✓
- Rotation Speed: 10

**Audio Settings:**
- Shoot Sound: (ваш звук выстрела)

4. **Настройте Input Actions** (см. раздел "Настройка Input System")

---

### 3. PlayerHealth
**Файл:** `PlayerHealth.cs`

#### Настройка:
1. На объекте игрока: `Add Component → PlayerHealth`
2. Настройки:

**Health:**
- Max Health: 10
- Start With Full Health: ✓
- Health Regen Enabled: ✓
- Health Regen Rate: 1 (HP в секунду)
- Health Regen Delay: 3 (секунды без урона)

**Shield:**
- Max Shield: 5
- Shield Regen Enabled: ✓
- Shield Regen Rate: 0.5
- Shield Regen Delay: 5

**Immunity:**
- Immunity Duration: 1.5 (секунды неуязвимости после урона)

**Respawn:**
- Auto Respawn: ✓
- Respawn Delay: 2
- Respawn Position: (0, 0, 0)

**Audio:**
- Hurt Sound: (звук получения урона)
- Death Sound: (звук смерти)
- Shield Break Sound: (звук разрушения щита)

---

### 4. HUDController
**Файл:** `HUDController.cs`

#### Настройка Canvas:
1. Создайте UI: `GameObject → UI → Canvas`
2. Canvas настройки:
   - Render Mode: Screen Space - Overlay
   - Canvas Scaler → UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920x1080

3. Создайте структуру:
```
Canvas
├── HealthBar (Image)
│   └── Fill (Image) - зеленый, Image Type: Filled
├── ScoreText (TextMeshPro)
├── WaveText (TextMeshPro)
├── ComboPanel (Panel)
│   ├── ComboText (TextMeshPro)
│   └── ComboMultiplierText (TextMeshPro)
├── AmmoText (TextMeshPro)
├── FPSText (TextMeshPro)
└── DamageEffect (Image) - красный, прозрачный
```

4. На Canvas добавьте: `Add Component → HUDController`
5. Перетащите все элементы в соответствующие поля Inspector

---

### 5. Enemy
**Файл:** `Enemy.cs`

#### Создание Enemy Prefab:
1. Создайте GameObject: `GameObject → 2D Object → Sprite`
2. Назовите "Enemy"
3. Добавьте компоненты:
   - `Rigidbody2D`
     - Gravity Scale: 0
     - Collision Detection: Continuous
     - Constraints: Freeze Rotation Z
   - `Collider2D` (Circle или Polygon)
   - `Add Component → Enemy`

4. Настройки Enemy:
- Max Health: 3
- Move Speed: 2
- Avoidance Radius: 1.5
- Shoot Range: 10
- Shoot Interval: 2
- Bullet Prefab: (EnemyBullet prefab)
- Fire Point: (Transform на врага)
- Bullet Speed: 8
- Score Value: 1

5. Установите Tag: "Enemy"
6. Сохраните как Prefab: перетащите в папку `Assets/Prefabs/`

---

### 6. EnemySpawner
**Файл:** `EnemySpawner.cs`

#### Настройка:
1. Создайте пустой GameObject: "EnemySpawner"
2. `Add Component → EnemySpawner`
3. Настройки:

**Spawn Settings:**
- Enemy Prefabs: (массив prefab'ов врагов)
- Spawn Interval: 2
- Max Enemies: 20

**Wave Settings:**
- Enable Waves: ✓
- Starting Wave: 1
- Base Enemies Per Wave: 5
- Enemies Per Wave Increase: 2
- Wave Break Duration: 5

**Spawn Patterns:**
- Available Patterns: (выберите Circle, Random, Grid, Wave, Spiral)

**Spawn Area:**
- Use Spawn Area: ✓
- Spawn Radius: 15
- Avoid Center: ✓
- Center Avoid Radius: 5

---

### 7. ScreenWrap2D
**Файл:** `ScreenWrap2D.cs`

#### Настройка:
1. На объекте игрока и врагах: `Add Component → ScreenWrap2D`
2. Настройки:
- Wrap Mode: Both Axes (или Horizontal Only)
- Padding: 1
- Account For Object Size: ✓
- Disable When Off Screen: false

---

### 8. UpgradeManager
**Файл:** `UpgradeManager.cs`

#### Настройка:
1. Создайте пустой GameObject: "UpgradeManager"
2. `Add Component → UpgradeManager`
3. Настройки:
- Player: (перетащите объект игрока)
- Player Health: (автоматически найдётся)
- Show Debug Logs: ✓

**Upgrade Values:**
- Move Speed Increase: 0.5
- Fire Rate Decrease: 0.05
- Bullet Speed Increase: 2
- Max Health Increase: 2
- Shield Restore: 3
- Health Restore: 5

**Upgrade Weights** (вероятность выпадения):
- Move Speed Weight: 10
- Fire Rate Weight: 10
- Bullet Speed Weight: 8
- Max Health Weight: 7
- Shield Restore Weight: 5
- Health Restore Weight: 5

---

## Этап 2: UI и Сохранения

### 9. PauseMenu
**Файл:** `PauseMenu.cs`

#### Создание UI:
1. В Canvas создайте:
```
Canvas
└── PausePanel (Panel) - темный полупрозрачный фон
    ├── TitleText (TextMeshPro) - "ПАУЗА"
    ├── ResumeButton (Button) - "Продолжить"
    ├── SettingsButton (Button) - "Настройки"
    ├── MainMenuButton (Button) - "Главное меню"
    └── SettingsPanel (Panel) - скрыт по умолчанию
        ├── MusicSlider (Slider)
        ├── SFXSlider (Slider)
        └── BackButton (Button)
```

2. На Canvas: `Add Component → PauseMenu`
3. Перетащите все элементы в Inspector
4. Pause Panel → Active: **ВЫКЛЮЧИТЕ** (скрыт по умолчанию)

---

### 10. GameOverScreen
**Файл:** `GameOverScreen.cs`

#### Создание UI:
1. В Canvas создайте:
```
Canvas
└── GameOverPanel (Panel) - черный полупрозрачный
    ├── GameOverText (TextMeshPro) - "GAME OVER"
    ├── ScoreText (TextMeshPro)
    ├── HighScoreText (TextMeshPro)
    ├── WaveText (TextMeshPro)
    ├── TimeText (TextMeshPro)
    ├── KillsText (TextMeshPro)
    ├── AccuracyText (TextMeshPro)
    ├── RestartButton (Button)
    ├── MainMenuButton (Button)
    └── QuitButton (Button)
```

2. На Canvas: `Add Component → GameOverScreen`
3. Настройки:
- Game Over Panel: (перетащите)
- Canvas Group: (добавьте CanvasGroup на GameOverPanel)
- Fade Duration: 1
- Все текстовые поля
- Кнопки

4. GameOverPanel → Active: **ВЫКЛЮЧИТЕ**

---

### 11. SaveManager
**Файл:** `SaveManager.cs`

#### Настройка:
1. Создайте пустой GameObject: "SaveManager"
2. `Add Component → SaveManager`
3. Настройки:
- Use Encryption: false (или true для защиты)
- Auto Save On Quit: ✓

**Использование в других скриптах:**
```csharp
// Сохранить очки
SaveManager.Instance.SaveHighScore(score);

// Сохранить настройки
SaveManager.Instance.SaveSettings(musicVolume, sfxVolume);

// Загрузить данные
SaveData data = SaveManager.Instance.LoadGame();
```

---

### 12. MainMenu
**Файл:** `MainMenu.cs`

#### Создание сцены MainMenu:
1. Создайте новую сцену: `File → New Scene`
2. Сохраните как "MainMenu" в `Assets/Scenes/`
3. Добавьте в Build Settings: `File → Build Settings → Add Open Scenes`

#### Создание UI:
```
Canvas
├── MainPanel (Panel)
│   ├── TitleText (TextMeshPro) - "NEON VOID"
│   ├── PlayButton (Button)
│   ├── SettingsButton (Button)
│   ├── StatsButton (Button)
│   ├── CreditsButton (Button)
│   └── QuitButton (Button)
├── SettingsPanel (Panel) - скрыт
│   ├── MusicSlider (Slider)
│   ├── SFXSlider (Slider)
│   └── BackButton (Button)
├── StatsPanel (Panel) - скрыт
│   ├── HighScoreText (TextMeshPro)
│   ├── GamesPlayedText (TextMeshPro)
│   ├── TotalKillsText (TextMeshPro)
│   ├── ResetButton (Button)
│   └── BackButton (Button)
└── CreditsPanel (Panel) - скрыт
    ├── CreditsText (TextMeshPro)
    └── BackButton (Button)
```

4. Создайте пустой GameObject: "MainMenu"
5. `Add Component → MainMenu`
6. Перетащите все панели и элементы
7. Game Scene Name: "GameScene" (имя вашей игровой сцены)

---

## Этап 3: Враги и Контент

### 13. EnemyTypes (TankEnemy, SniperEnemy, KamikazeEnemy)
**Файл:** `EnemyTypes.cs`

#### Создание Tank Enemy:
1. Дублируйте базовый Enemy prefab
2. Назовите "TankEnemy"
3. **ЗАМЕНИТЕ** компонент Enemy на: `Add Component → Tank Enemy`
4. Настройки:
- Max Health: 10
- Move Speed: 1
- Charge Speed: 8
- Charge Distance: 10
- Charge Cooldown: 5
- Score Value: 5
5. Увеличьте спрайт на 20-30%
6. Цвет: серый/металлический

#### Создание Sniper Enemy:
1. Дублируйте базовый Enemy prefab
2. Назовите "SniperEnemy"
3. **ЗАМЕНИТЕ** Enemy на: `Add Component → Sniper Enemy`
4. Настройки:
- Max Health: 2
- Move Speed: 3
- Bullet Speed: 15
- Shoot Range: 20
- Shoot Interval: 3
- Keep Distance: 12
- Bullet Prefab: (EnemyBullet)
- Fire Point: (Transform)
- Score Value: 3
5. Цвет: синий/фиолетовый

#### Создание Kamikaze Enemy:
1. Дублируйте базовый Enemy prefab
2. Назовите "KamikazeEnemy"
3. **ЗАМЕНИТЕ** Enemy на: `Add Component → Kamikaze Enemy`
4. Настройки:
- Max Health: 1
- Move Speed: 2
- Rush Speed: 6
- Activation Range: 8
- Explosion Radius: 3
- Explosion Damage: 2
- Score Value: 2
5. Цвет: красный/оранжевый

---

### 14. BossEnemy
**Файл:** `BossEnemy.cs`

#### Создание Boss Prefab:
1. Создайте новый GameObject: "Boss"
2. Спрайт: большой (в 3-4 раза больше обычного врага)
3. Компоненты:
   - `Rigidbody2D` (Gravity: 0, Continuous)
   - `Collider2D`
   - `Add Component → Boss Enemy`

4. Настройки:
- Max Health: 100
- Move Speed: 1.5
- Score Value: 100

**Фазы:**
- Phase 1 Health Threshold: 66%
- Phase 2 Health Threshold: 33%

**Атаки:**
- Bullet Prefab: (EnemyBullet)
- Fire Points: (создайте несколько Transform-ов вокруг босса)
- Bullet Speed: 8
- Spread Bullet Count: 8
- Spread Interval: 2

**Миньоны:**
- Minion Prefabs: (массив врагов)
- Minions Per Phase: 3
- Minion Spawn Radius: 5

**UI:**
- Health Bar Prefab: (создайте BossHealthBar UI)

5. Tag: "Enemy"

#### Создание BossHealthBar UI:
1. В Canvas создайте:
```
Canvas (DontDestroyOnLoad)
└── BossHealthBarPanel (Panel) - вверху экрана
    ├── BossNameText (TextMeshPro)
    ├── HealthBarBackground (Image)
    │   └── HealthBarFill (Image) - красный градиент
    └── HealthText (TextMeshPro)
```

2. На Panel: `Add Component → Boss Health Bar`
3. Настройте Health Gradient:
   - 0%: красный
   - 50%: желтый
   - 100%: зеленый

4. Сохраните как Prefab: "BossHealthBar"
5. Перетащите в Boss Enemy → Health Bar Prefab

---

### 15. WeaponManager
**Файл:** `WeaponManager.cs`

#### Настройка:
1. На объекте игрока: `Add Component → Weapon Manager`
2. Настройте Weapon Configs (массив из 5 элементов):

**Element 0 - Standard:**
- Type: Standard
- Bullet Prefab: (обычная пуля)
- Fire Rate: 0.2
- Bullet Speed: 15
- Bullet Count: 1
- Shoot Sound: (звук)

**Element 1 - Spread:**
- Type: Spread
- Bullet Prefab: (обычная пуля)
- Fire Rate: 0.3
- Bullet Speed: 12
- Bullet Count: 3
- Spread Angle: 15
- Shoot Sound: (звук)

**Element 2 - Rapid:**
- Type: Rapid
- Bullet Prefab: (обычная пуля)
- Fire Rate: 0.1
- Bullet Speed: 18
- Bullet Count: 1
- Shoot Sound: (звук)

**Element 3 - Laser:**
- Type: Laser
- Bullet Prefab: (не нужен)
- Fire Rate: 0.02
- Shoot Sound: (звук лазера)

**Element 4 - Homing:**
- Type: Homing
- Bullet Prefab: (обычная пуля)
- Fire Rate: 0.4
- Bullet Speed: 10
- Bullet Count: 1
- Shoot Sound: (звук)

**Laser Settings:**
- Laser Line: (создайте Line Renderer на игроке)
  - Width: 0.1-0.2
  - Material: яркий светящийся
  - Color: Gradient (белый → синий)
  - Sort Order: 10
- Laser Max Distance: 50
- Laser Damage Per Second: 10
- Laser Targets: (LayerMask с врагами)

3. Fire Point: (Transform на носу корабля)
4. Player Controller: (автоматически найдётся)

**Использование:**
```csharp
WeaponManager wm = GetComponent<WeaponManager>();
wm.SetWeapon(WeaponManager.WeaponType.Spread);
```

---

### 16. LevelObstacles
**Файл:** `LevelObstacles.cs`

#### Создание Destructible Obstacle:
1. Создайте GameObject с спрайтом (астероид, ящик и т.д.)
2. Компоненты:
   - `Collider2D`
   - `Add Component → Level Obstacle`
3. Настройки:
- Obstacle Type: Destructible
- Max Health: 5
- Score Value: 1
- Power Up Prefab: (опционально)
- Drop Chance: 0.3
- Hit Sound, Destroy Sound

#### Создание Bouncy Obstacle:
1. То же самое
2. Obstacle Type: Bouncy
3. Bounce Force: 1.5

#### Создание Rotating Obstacle:
1. То же самое
2. Obstacle Type: Rotating
3. Rotation Speed: 45

#### Создание Moving Obstacle:
1. То же самое
2. Obstacle Type: Moving
3. Move Direction: (1, 0) - вправо
4. Move Speed: 2
5. Move Distance: 5

#### Создание Hazard:
1. Создайте GameObject (лава, электричество и т.д.)
2. Компоненты:
   - `Collider2D` (Is Trigger: ✓)
   - `Add Component → Hazard`
3. Настройки:
- Damage Amount: 1
- Damage Cooldown: 1
- Damage Sound

---

## Настройка Input System

### Создание Input Actions:
1. В папке Assets создайте: `Right Click → Create → Input Actions`
2. Назовите "InputSystem_Actions"
3. Откройте двойным кликом

4. Создайте Action Map: "Player"
5. Добавьте Actions:

**Move:**
- Action Type: Value
- Control Type: Vector2
- Binding: WASD или Arrow Keys
- Composite: 2D Vector

**Shoot:**
- Action Type: Button
- Binding: Left Mouse Button

**Pause:**
- Action Type: Button
- Binding: Escape

6. Нажмите "Generate C# Class"
7. Сохраните: `Ctrl + S`

### Подключение к PlayerController:
1. Выберите игрока
2. PlayerController → Input Actions: перетащите InputSystem_Actions

---

## Создание Prefab'ов

### Обязательные Prefabs:
1. **Player** - корабль игрока со всеми компонентами
2. **PlayerBullet** - пуля игрока (Rigidbody2D, Collider, Tag: "Bullet")
3. **Enemy** - базовый враг
4. **TankEnemy** - танк враг
5. **SniperEnemy** - снайпер
6. **KamikazeEnemy** - камикадзе
7. **Boss** - босс
8. **EnemyBullet** - пуля врага (Rigidbody2D, Collider, EnemyBullet.cs)
9. **PowerUp** - бонус (если используете PowerUpManager)
10. **Obstacle_Destructible** - разрушаемое препятствие
11. **Obstacle_Bouncy** - отражающее препятствие
12. **Hazard** - опасность

### Создание Prefab:
1. Настройте GameObject в сцене
2. Перетащите из Hierarchy в папку `Assets/Prefabs/`
3. Удалите из сцены (останется только prefab)

---

## Настройка Сцен

### GameScene (основная игра):
```
Hierarchy:
├── Main Camera
├── EventSystem
├── Canvas (HUDController, PauseMenu, GameOverScreen)
├── Player (PlayerController, PlayerHealth, WeaponManager, ScreenWrap2D)
├── AudioManager
├── EnemySpawner
├── UpgradeManager
├── SaveManager
├── PowerUpManager (если используется)
├── ParticleManager (если используется)
├── ObjectPoolManager (если используется)
└── Level (препятствия, фон и т.д.)
```

### MainMenu Scene:
```
Hierarchy:
├── Main Camera
├── EventSystem
├── Canvas (MainMenu UI)
├── AudioManager
└── SaveManager
```

### Build Settings:
1. `File → Build Settings`
2. Добавьте сцены в правильном порядке:
   - [0] MainMenu
   - [1] GameScene

---

## Важные Теги и Слои

### Tags (Edit → Project Settings → Tags and Layers):
- Player
- Enemy
- Bullet
- EnemyBullet
- PowerUp
- Obstacle

### Layers:
- Player (layer 6)
- Enemy (layer 7)
- Bullet (layer 8)
- Obstacle (layer 9)

### Physics 2D Matrix (Edit → Project Settings → Physics 2D):
Настройте коллизии:
- Player ✓ Enemy, EnemyBullet, Obstacle
- Enemy ✓ Bullet, Obstacle
- Bullet ✓ Enemy, Obstacle
- EnemyBullet ✓ Player, Obstacle

---

## Порядок Внедрения

### День 1: Базовые системы
1. ✅ Настройте Input System
2. ✅ AudioManager
3. ✅ PlayerController + PlayerHealth
4. ✅ HUDController
5. ✅ Создайте Player prefab
6. ✅ Протестируйте движение и стрельбу

### День 2: Враги
1. ✅ Enemy базовый + EnemyBullet
2. ✅ EnemySpawner
3. ✅ ScreenWrap2D на всех
4. ✅ UpgradeManager
5. ✅ Протестируйте волны врагов

### День 3: UI
1. ✅ PauseMenu
2. ✅ GameOverScreen
3. ✅ SaveManager
4. ✅ MainMenu сцена
5. ✅ Протестируйте навигацию

### День 4: Контент
1. ✅ EnemyTypes (Tank, Sniper, Kamikaze)
2. ✅ WeaponManager
3. ✅ LevelObstacles
4. ✅ Добавьте в EnemySpawner все типы врагов
5. ✅ Протестируйте разнообразие

### День 5: Boss
1. ✅ BossEnemy
2. ✅ BossHealthBar UI
3. ✅ Создайте отдельную волну для босса
4. ✅ Протестируйте фазы

---

## Частые Ошибки и Решения

### ❌ "NullReferenceException: Object reference not set"
**Решение:** Проверьте, что все поля в Inspector заполнены (prefab'ы, AudioClip'ы, UI элементы)

### ❌ "The object of type 'X' has been destroyed but you are still trying to access it"
**Решение:** Проверьте условие `!= null` перед обращением к объектам

### ❌ Враги не спавнятся
**Решение:** 
- Проверьте Enemy Prefabs в EnemySpawner
- Убедитесь, что EnemySpawner активен
- Проверьте Spawn Area

### ❌ Пули не наносят урон
**Решение:**
- Проверьте Tags ("Bullet", "Enemy", "Player")
- Проверьте Layers и Physics 2D Matrix
- Убедитесь, что на пулях и врагах есть Collider2D

### ❌ Input не работает
**Решение:**
- Проверьте, что InputSystem_Actions сгенерирован
- Убедитесь, что Input Actions включен в PlayerController
- Проверьте, что в проекте установлен Input System пакет

### ❌ AudioManager.Instance == null
**Решение:**
- AudioManager должен быть в первой загружаемой сцене
- Проверьте, что на нём стоит скрипт AudioManager.cs
- Убедитесь, что он не удаляется при смене сцены

---

## Финальная Проверка

### Чеклист перед запуском:
- [ ] Все prefab'ы созданы и настроены
- [ ] Теги установлены на Player, Enemy, Bullet
- [ ] Layers настроены в Physics 2D Matrix
- [ ] Input System настроен и работает
- [ ] AudioManager в сцене с AudioSource'ами
- [ ] Canvas с HUDController настроен
- [ ] Все UI элементы перетащены в Inspector
- [ ] MainMenu и GameScene в Build Settings
- [ ] SaveManager и AudioManager с DontDestroyOnLoad
- [ ] Протестирован полный игровой цикл

---

## 🎯 Готово!

После выполнения всех шагов у вас будет полноценная игра с:
- ✅ Управлением и стрельбой
- ✅ Системой здоровья и щита
- ✅ Волнами врагов
- ✅ 3 типами врагов + босс
- ✅ 5 типами оружия
- ✅ Препятствиями
- ✅ UI и меню
- ✅ Сохранениями
- ✅ Звуком и музыкой

**Удачи! 🚀**

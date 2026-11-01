# 🏭 I Need More Profit!

**Industrial City-Building & Economic Tycoon Simulation**  
*(Endüstriyel Şehir Kurma ve Ekonomi Simülasyonu — Hackathon Project)*

> 🏆 **Hackathon Project:** Designed and developed from scratch within a limited hackathon timeframe using **Unity 6 (2D URP)** and **Unity Version Control (UVCS / Plastic SCM)**, blending classic industrial tycoon mechanics with real-time environmental and atmospheric storytelling.

---

## 🌍 Overview | Oyun Hakkında

**EN:**  
**I Need More Profit!** is a 2D isometric/top-down city-building and industrial management simulation developed as a **Hackathon Project** in **Unity 6 (2D URP)**. Step into the shoes of a newly appointed Mayor, construct factories, expand housing for your growing workforce, upgrade production lines, and chase record-breaking profits—while keeping an eye on your city's **Air Quality Index (AQI)** and **Citizen Happiness**.

**TR:**  
**I Need More Profit! (Daha Fazla Kâr Lazım!)**, bir **Hackathon Projesi** olarak **Unity 6 (2D URP)** ile geliştirilmiş 2B şehir kurma ve sanayi yönetimi simülasyonudur. Belediye Başkanı olarak fabrikalar kurun, büyüyen iş gücünüz için konutlar inşa edin, üretim hatlarını Seviye 2 ve 3'e yükseltin ve rekor kârlara ulaşın—ancak şehrinizi büyütürken **Hava Kalitesi İndeksi (AQI)** ve **Vatandaş Mutluluğu** değerlerine dikkat edin.

---

## ✨ Key Features | Temel Özellikler

* **🏗️ Industrial & Workforce Management:** Build and upgrade **Houses** (`Level 1–3`) to expand population capacity and **Factories** (`Level 1–3`) to multiply your treasury income.
* **📊 Realistic AQI & Dynamic Ecosystem:**
  * Multi-stage environmental visuals where trees (`Tree A`, `Tree B`), bushes, grass, and ground tiles dynamically react to air quality (`Good` → `Moderate` → `Unhealthy` → `Hazardous`).
  * Dynamic **Sky Bird Flocks** (`BirdFlockManager`) that fly across clean skies and gradually vanish as industrial pollution rises.
  * Soft procedural ground shadows and atmospheric smog transitions.
* **🗣️ Interactive City Advisor:** Bottom-left character advisor (`AdvisorManager`) with Animal Crossing / Stardew Valley-style typewriter dialogue, pitch-modulated voice blips, and context-aware city alerts.
* **📰 Milestone Newspapers:** Spinning special-edition newspapers (`THE DAILY INDUSTRIAL` / `SANAYİ POSTASI`) that document your city's key industrial and environmental turning points.
* **🎵 Psychological Dynamic Audio:** 4-layer adaptive soundscape (`AudioManager`) that smoothly crossfades and pitch-shifts from peaceful birdsong into tense industrial/dystopian ambience based on real-time AQI and citizen happiness.
* **🌐 Full Bilingual Support (EN / TR):** Instant in-game localization toggle between **English** and **Turkish** (`LocalizationManager`).
* **🏆 Run History & Profit Records:** Persistent tracking of **Best Peak Profit**, **Final AQI**, and **Survival Time** across runs.

---

## 🎮 Controls & How to Play | Kontroller ve Oynanış

| Action / İşlem | Input / Kontrol |
| :--- | :--- |
| **Pan Camera / Kamera Gezinme** | `Right-Click + Drag` (Sağ Tık Basılı Tut & Sürükle) |
| **Zoom In & Out / Yakınlaştırma** | `Mouse Scroll Wheel` (Fare Tekerleği) |
| **Build / Upgrade / Demolish** | `Left-Click` on bottom-right action toolbar & grid |
| **Pause & Executive Menu** | `ESC` or top-right `MENU [ESC]` button |
| **Language Toggle (EN / TR)** | Top-right `EN / TR` button or Main Menu |

---

## 🛠️ Tech Stack & Version Control | Teknik Altyapı ve Süreç

* **Engine:** Unity 6 (`6000.5.2f1`) — Universal Render Pipeline (2D URP)
* **Language:** C# (`TextMeshPro`, `Unity Input System`, Procedural Audio & Sprite Generation)
* **Version Control & Collaboration:**
  * Primary team development, scene merging, and binary asset synchronization were managed via **Unity Version Control (UVCS / Plastic SCM)** through Unity Cloud Organization.
  * Mirrored to **GitHub** with a clean Unity `.gitignore` structure (`Assets/`, `Packages/`, `ProjectSettings/`), alongside standalone **Windows** and browser-based **WebGL (HTML5)** builds for cross-platform live demo access.

---

## 📂 Project Structure | Proje Yapısı

```text
├── Assets/
│   ├── Prefabs/          # Building & Nature prefabs (Factory, House, Trees, Bush, Grass, Tile)
│   ├── Resources/        # Build-embedded Audio, Advisor portraits, and Fonts
│   ├── Scenes/           # Main game scene (SampleScene.unity)
│   ├── Scripts/          # Core gameplay & UI architecture
│   │   ├── GameManager.cs          # Turn loop, AQI/Happiness math, pacing & environment state
│   │   ├── BuildingManager.cs      # Grid placement, upgrades, and demolition
│   │   ├── AdvisorManager.cs       # Typewriter advisor character & ending monologue
│   │   ├── NewspaperManager.cs     # Milestone newspaper popups & animations
│   │   ├── QuestAndMenuManager.cs  # Executive full-screen menu, quests & localization UI
│   │   ├── AudioManager.cs         # Dynamic multi-layer audio & pitch shifting
│   │   ├── BirdFlockManager.cs     # AQI-responsive sky bird flocks
│   │   ├── NatureVisuals.cs        # Multi-stage nature sprites & drop shadows
│   │   └── LocalizationManager.cs  # Instant EN / TR language switching
│   ├── Sounds/           # Ambient loops & sound effects
│   └── Sprites/          # 2D pixel-art buildings, nature stages, and character sprites
├── Packages/             # Unity Package Manager manifest
└── ProjectSettings/      # Unity project & URP settings (Product Name: I Need More Profit!)
```

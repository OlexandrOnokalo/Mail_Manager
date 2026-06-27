# Mail Manager — Технічна документація проєкту

> **Версія документа:** 1.0  
> **Мова:** Українська  
> **Цільова аудиторія:** Розробник, рев'юер, інтерв'юер  
> ⚠️ Усі облікові дані (email, паролі, рядки підключення) замінено на `[EMAIL]`, `[PASSWORD]`, `[CONNECTION_STRING]`.

---

## 1. ОГЛЯД ПРОЄКТУ

### Задача та цільова аудиторія

**Mail Manager** — це настільний email-клієнт для Windows, розроблений як навчальний груповий проєкт. Додаток дозволяє авторизованому користувачу:

- переглядати пошту з Gmail-акаунту через протокол IMAP;
- читати, видаляти листи та переміщатися між папками поштової скриньки;
- складати й надсилати нові листи (у т.ч. з вкладеннями) через SMTP;
- відповідати на отримані листи (Reply);
- зберігати облікові записи у зовнішній базі даних SQL Server.

**Цільовий користувач:** один або кілька заздалегідь зареєстрованих Gmail-акаунтів.

---

### Архітектура проєкту: Layered Architecture (Code-Behind + Service Layer)

Проєкт використовує **Layered Architecture** (шарову архітектуру) у спрощеній формі:

| Шар | Що входить | Роль |
|---|---|---|
| **Presentation (UI)** | `Windows/*.xaml` + code-behind `*.xaml.cs` | Відображення та взаємодія з користувачем |
| **Service Layer** | `Services/ImapService.cs`, `Services/SmtpService.cs` | Бізнес-логіка + протокольна взаємодія |
| **Domain/Model** | `Models/EmailItem.cs`, `Models/SessionState.cs` | Доменні об'єкти та стан сесії |
| **Data Access** | `Mail_Data_Access/MailDbContext.cs`, `Mail_Data_Access/Models/User.cs` | Робота з БД через EF Core |

Це хороший вибір для навчального WPF-додатку: шари чітко розділені, залежності йдуть в одному напрямку (UI → Service → DAL), і при цьому не ускладнюють проєкт зайвою абстракцією (як-от MVVM з повноцінними ViewModel).

---

### Tech Stack та обґрунтування бібліотек

| Технологія / Бібліотека | Версія | Роль у проєкті | Обґрунтування вибору |
|---|---|---|---|
| **.NET 8.0-windows** | 8.0 | Цільовий фреймворк WPF-додатку | LTS-реліз .NET з підтримкою WPF; `net8.0-windows` — єдиний TFM, що підтримує WPF-специфічні API |
| **WPF** (Windows Presentation Foundation) | вбудовано в .NET 8 | UI-фреймворк | Стандартний вибір для Windows-десктоп із потужним XAML-лейаутом та databinding |
| **MailKit** | 4.17.0 | IMAP/SMTP клієнт | Найбільш зрілий і функціональний open-source email-клієнт для .NET; підтримує SSL/TLS, SASL, асинхронні операції, папки IMAP, унікальні ID. Рекомендований Microsoft замість застарілого `System.Net.Mail` |
| **MimeKit** | 4.17.0 | Парсинг і побудова MIME-повідомлень | Нижньорівнева бібліотека під капотом MailKit; дозволяє будувати складні повідомлення (`BodyBuilder`, вкладення, multipart) |
| **MaterialDesignThemes** | 5.3.0 | UI-тема та компоненти | Реалізує Google Material Design для WPF; дає сучасний вигляд без написання власних стилів; включає готові `MaterialDesignWindow`, кнопки, поля введення |
| **Microsoft.EntityFrameworkCore** | 8.0.22 | ORM для доступу до БД | Code-first підхід із міграціями; зменшує обсяг boilerplate SQL; повністю інтегрується з .NET DI |
| **Microsoft.EntityFrameworkCore.SqlServer** | 8.0.22 | Провайдер для SQL Server | Офіційний провайдер EF Core від Microsoft; підтримує хмарний SQL (Somee) |
| **Microsoft.EntityFrameworkCore.Design** | 8.0.22 | Інструменти design-time для міграцій | Потрібен для `dotnet ef migrations add` / `dotnet ef database update` |
| **Microsoft.EntityFrameworkCore.Tools** | 8.0.22 | PMC-команди для міграцій у VS | Потрібен для роботи в Package Manager Console у Visual Studio |

---

### Загальна архітектура — ASCII-схема

```
┌─────────────────────────────────────────────────────────────┐
│                     Mail_Manager.exe                        │
│                  (net8.0-windows, WPF)                      │
│                                                             │
│  ┌─────────────────────────── UI LAYER ─────────────────┐   │
│  │  App.xaml             StartupUri → LoginWindow       │   │
│  │  LoginWindow.xaml     ← Вхідна точка UI              │   │
│  │  MainWindow.xaml      ← Головне вікно (папки+листи)  │   │
│  │  ComposeWindow.xaml   ← Написати лист                │   │
│  │  ViewMessageWindow.xaml ← Читання + Reply            │   │
│  └──────────────┬──────────────────────────┬────────────┘   │
│                 │ використовує             │ читає/пише     │
│  ┌──────────────▼──────────┐   ┌──────────▼─────────────┐   │
│  │     SERVICE LAYER       │   │      MODELS             │   │
│  │  ImapService (instance) │   │  EmailItem (DTO)        │   │
│  │  SmtpService (static)   │   │  SessionState (static)  │   │
│  └──────────────┬──────────┘   └────────────────────────┘   │
│                 │ MailKit / MimeKit                          │
│                 ▼                                           │
│         ┌──────────────┐   ┌──────────────┐                 │
│         │  Gmail IMAP  │   │  Gmail SMTP  │                 │
│         │ imap.gmail.com│  │smtp.gmail.com│                 │
│         │    :993/SSL   │   │  :587/STARTTLS│               │
│         └──────────────┘   └──────────────┘                 │
│                                                             │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                   Mail_Data_Access.dll                      │
│                      (net8.0)                               │
│                                                             │
│  ┌─────────────────── DATA ACCESS LAYER ────────────────┐   │
│  │  MailDbContext (EF Core DbContext)                   │   │
│  │  Models/User.cs (Entity: Id, Mail, Password)        │   │
│  │  Migrations/20251112151453_init.cs                  │   │
│  └──────────────────────┬───────────────────────────────┘   │
│                         │ EF Core + SqlServer Provider      │
│                         ▼                                   │
│              ┌──────────────────────┐                       │
│              │ SQL Server (Somee)   │                       │
│              │  Table: Users        │                       │
│              └──────────────────────┘                       │
└─────────────────────────────────────────────────────────────┘

  Mail_Manager ──ProjectReference──▶ Mail_Data_Access
```

---

## 2. КАРТА ПРОЄКТУ ПО ФАЙЛАХ

### Модуль: Точка входу

#### `Mail_Manager/App.xaml` + `App.xaml.cs`
- **Призначення:** Точка запуску WPF-додатку. Декларує глобальні ресурси теми Material Design.
- **Ключові деталі:**
  - `StartupUri="Windows/LoginWindow.xaml"` — перше вікно, яке відкривається.
  - `<materialDesign:BundledTheme BaseTheme="Dark" PrimaryColor="BlueGrey" SecondaryColor="Lime"/>` — темна тема.
  - `App.xaml.cs` — порожній (без кастомної логіки запуску).
- **Взаємодіє з:** `LoginWindow.xaml`, `MaterialDesignThemes` (NuGet).

---

### Модуль: Моделі даних

#### `Mail_Manager/Models/EmailItem.cs`
- **Призначення:** DTO (Data Transfer Object) для відображення листа у списку головного вікна. Містить лише ті дані, що потрібні для рядка у `ListView`.
- **Властивості:**
  - `UniqueId` (`MailKit.UniqueId`) — стабільний ідентифікатор листа на сервері (IMAP UID); використовується для отримання повного тіла та видалення.
  - `From` (`string`) — рядок відправника.
  - `Subject` (`string`) — тема листа.
  - `Date` (`DateTimeOffset`) — дата отримання.
- **Взаємодіє з:** `ImapService.GetPageAsync()` (де створюється), `MainWindow` (де відображається).

#### `Mail_Manager/Models/SessionState.cs`
- **Призначення:** Статичний клас-синглтон для зберігання облікових даних поточного сеансу. Дозволяє `ComposeWindow` отримати email і пароль без передачі через конструктор.
- **Властивості:**
  - `Email` (`static string`) — email авторизованого користувача.
  - `Password` (`static string`) — App Password для SMTP/IMAP.
- **Встановлюється в:** `LoginWindow.BtnLogin_Click`.
- **Читається в:** `ComposeWindow.BtnSend_Click`.

---

### Модуль: Сервіси

#### `Mail_Manager/Services/ImapService.cs`
- **Призначення:** Інкапсулює всю логіку роботи з IMAP-протоколом. Реалізує `IAsyncDisposable` для коректного закриття з'єднання.
- **Ключові поля:**
  - `_client` (`ImapClient`) — єдиний IMAP-клієнт; живе протягом усього сеансу.
  - `Client` (публічна властивість) — надає доступ до `ImapClient.Inbox` з `MainWindow`.
- **Ключові методи:**

| Метод | Підпис | Що робить |
|---|---|---|
| `ConnectAsync` | `(string email, string password, CancellationToken)` | Підключається до `imap.gmail.com:993` (SSL), видаляє XOAUTH2, автентифікується |
| `GetAllSelectableFoldersAsync` | `(CancellationToken)` | Рекурсивно обходить дерево папок, відкриває кожну у ReadOnly, дедуплікує за FullName |
| `GetPageRange` | `static (int total, int pageIndex, int pageSize)` | Обчислює індекси `start`/`end` для отримання сторінки листів (з кінця, бо нові листи мають найбільший індекс) |
| `GetPageAsync` | `(IMailFolder, int pageIndex, int pageSize, CancellationToken)` | Завантажує зведення (envelope + date) для сторінки; повертає `IList<EmailItem>` |
| `GetMessageAsync` | `(IMailFolder, UniqueId, CancellationToken)` | Завантажує повне `MimeMessage` за UID |
| `DeleteAsync` | `(IMailFolder, UniqueId, CancellationToken)` | Відкриває папку ReadWrite → виставляє прапор `Deleted` → `Expunge` → знову відкриває ReadOnly |
| `DisposeAsync` | — | Відключається від IMAP і dispose-ить клієнт |

- **Взаємодіє з:** `MailKit.Net.Imap`, `MimeKit`, `MainWindow`, `ViewMessageWindow`.

#### `Mail_Manager/Services/SmtpService.cs`
- **Призначення:** Статичний сервіс для одноразового надсилання листа через SMTP.
- **Ключові методи:**

| Метод | Підпис | Що робить |
|---|---|---|
| `SendAsync` | `static (string fromEmail, string password, MimeMessage, CancellationToken)` | Створює `SmtpClient`, підключається до `smtp.gmail.com:587` (STARTTLS), видаляє XOAUTH2, автентифікується, надсилає, відключається |

- **Взаємодіє з:** `MailKit.Net.Smtp`, `MimeKit`, `ComposeWindow`.

---

### Модуль: Вікна (UI)

#### `Mail_Manager/Windows/LoginWindow.xaml` + `.xaml.cs`
- **Призначення:** Перше вікно додатку. Приймає email і пароль, перевіряє їх у БД, ініціює IMAP-з'єднання.
- **Ключові елементи UI:** `TextBox Email`, `TextBox Password`, `Button LOGIN` (Grid 4×4).
- **Ключові методи:**

| Метод | Що робить |
|---|---|
| `LoginWindow()` (конструктор) | Ініціалізує `MailDbContext db`; попередньо заповнює поля для зручності тестування (значення замінено на `[EMAIL]` / `[PASSWORD]`) |
| `BtnLogin_Click` | Перевіряє `db.Users` на наявність пари email+password → якщо знайдено, створює `ImapService`, викликає `ConnectAsync`, записує в `SessionState`, відкриває `MainWindow`, закриває себе |

- **Взаємодіє з:** `MailDbContext`, `ImapService`, `SessionState`, `MainWindow`.

#### `Mail_Manager/Windows/MainWindow.xaml` + `.xaml.cs`
- **Призначення:** Головне вікно. Показує список папок ліворуч і список листів праворуч. Реалізує пагінацію.
- **Ключові поля:**
  - `_imap` (`ImapService`) — отримується через конструктор (ін'єкція залежності через параметр).
  - `_folders` (`IList<IMailFolder>`) — усі папки поштової скриньки.
  - `_currentFolder` (`IMailFolder?`) — активна папка.
  - `_pageIndex` (`int`) — поточна сторінка (0-based).
  - `PageSize = 20` (`const`) — кількість листів на сторінці.
- **Ключові методи:**

| Метод | Що робить |
|---|---|
| `MainWindow_Loaded` | Викликає `LoadFoldersAsync` при завантаженні вікна |
| `LoadFoldersAsync` | Отримує всі папки, сортує за `FullName`, автоматично обирає `INBOX` |
| `LstFolders_SelectionChanged` | При зміні папки — скидає `_pageIndex = 0`, оновлює список листів |
| `LoadPageAsync` | Викликає `_imap.GetPageAsync`, прив'язує результат до `lstMessages` |
| `BtnPrev_Click` / `BtnNext_Click` | Навігація по сторінках; Prev перевіряє `_pageIndex > 0` |
| `BtnOpen_Click` | Отримує повний `MimeMessage` → відкриває `ViewMessageWindow` |
| `BtnDelete_Click` | Викликає `_imap.DeleteAsync` → перезавантажує сторінку |
| `BtnNew_Click` | Відкриває `ComposeWindow` як модальний діалог |

- **Взаємодіє з:** `ImapService`, `EmailItem`, `ViewMessageWindow`, `ComposeWindow`.

#### `Mail_Manager/Windows/ComposeWindow.xaml` + `.xaml.cs`
- **Призначення:** Модальне вікно для написання нового листа або відповіді (Reply). Підтримує вкладення файлів.
- **Ключові поля:**
  - `_attachments` (`List<string>`) — шляхи до доданих файлів.
- **Конструктор:** `ComposeWindow(string to = "", string subject = "")` — може бути ініційований з попередньо заповненими полями (для Reply).
- **Ключові методи:**

| Метод | Що робить |
|---|---|
| `BtnAttach_Click` | Відкриває `OpenFileDialog` (Multiselect=true), додає шляхи в `_attachments` |
| `BtnSend_Click` | Читає `SessionState.Email/Password`, будує `MimeMessage` через `BodyBuilder`, додає вкладення, викликає `SmtpService.SendAsync` |
| `BtnCancel_Click` | Закриває вікно |

- **Взаємодіє з:** `SessionState`, `SmtpService`, `MimeKit.MimeMessage`, `MimeKit.BodyBuilder`.

#### `Mail_Manager/Windows/ViewMessageWindow.xaml` + `.xaml.cs`
- **Призначення:** Модальне вікно для читання повного листа. Показує From, To, Subject, Date, TextBody/HtmlBody. Дає змогу відповісти.
- **Конструктор:** Приймає `ImapService`, `IMailFolder`, `MimeMessage`, `UniqueId` — одразу заповнює всі текстові поля.
- **Ключові методи:**

| Метод | Що робить |
|---|---|
| `BtnReply_Click` | Визначає адресу відповіді (ReplyTo або From), додає префікс "Re:", відкриває `ComposeWindow` |
| `BtnClose_Click` | Закриває вікно |

- **Взаємодіє з:** `MimeMessage`, `ComposeWindow`.

---

### Модуль: Data Access

#### `Mail_Data_Access/MailDbContext.cs`
- **Призначення:** EF Core `DbContext` для роботи з таблицею `Users` у SQL Server.
- **Ключові елементи:**
  - `DbSet<User> Users` — представляє таблицю Users.
  - `OnConfiguring` — hard-coded рядок підключення до хмарного SQL Server (Somee). Локальний варіант залишений у коментарі.
  - `OnModelCreating` — seed-data: один користувач з `[EMAIL]` / `[PASSWORD]` (замінено).
- **Взаємодіє з:** `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.SqlServer`, `User`.

#### `Mail_Data_Access/Models/User.cs`
- **Призначення:** Entity-клас для таблиці `Users`.
- **Властивості:** `Id` (int, PK, IDENTITY), `Mail` (string?), `Password` (string?).

#### `Mail_Data_Access/Migrations/20251112151453_init.cs`
- **Призначення:** Перша (і єдина) міграція EF Core. Створює таблицю `Users` із seed-записом.

---

## 3. КЛЮЧОВІ ПОТОКИ (STEP-BY-STEP)

### 3.1 Авторизація та ініціалізація сеансу

```
Крок 1: App.xaml → StartupUri → LoginWindow.xaml відкривається
Крок 2: LoginWindow() конструктор: MailDbContext db = new MailDbContext()
		← підключення до SQL Server встановлюється ліниво
Крок 3: Користувач вводить [EMAIL] та [PASSWORD] → натискає LOGIN
Крок 4: LoginWindow.BtnLogin_Click
		→ db.Users.Where(u => u.Mail == email && u.Password == password).FirstOrDefault()
		← якщо null → MessageBox "no user"
Крок 5: якщо user != null → new ImapService()
Крок 6: await imap.ConnectAsync(email, password)
		→ ImapService.ConnectAsync:
		  → _client.ConnectAsync("imap.gmail.com", 993, SslOnConnect)
		  → _client.AuthenticationMechanisms.Remove("XOAUTH2")
		  → _client.AuthenticateAsync(email, password)
Крок 7: SessionState.Email = email; SessionState.Password = password
Крок 8: new MainWindow(imap) → main.Show() → this.Close()
```

**Файли:** `LoginWindow.xaml.cs` → `ImapService.ConnectAsync` → `SessionState` → `MainWindow`

---

### 3.2 Завантаження папок та листів (Read Flow)

```
Крок 1: MainWindow_Loaded → LoadFoldersAsync()
Крок 2: ImapService.GetAllSelectableFoldersAsync()
		→ додає Inbox до списку
		→ отримує PersonalNamespaces[0]
		→ рекурсивно обходить: RecurseAsync(folder)
		  → якщо !FolderAttributes.NoSelect → додає до result
		  → для кожного subfolder → рекурсія
		→ DistinctBy(f => f.FullName)
		→ для кожної папки: f.OpenAsync(FolderAccess.ReadOnly)
Крок 3: lstFolders.ItemsSource = folders.OrderBy(f => f.FullName)
Крок 4: автоматично обирається INBOX → LstFolders_SelectionChanged
Крок 5: _currentFolder = inbox; _pageIndex = 0 → LoadPageAsync()
Крок 6: ImapService.GetPageAsync(folder, pageIndex=0, pageSize=20)
		→ GetPageRange(total, 0, 20) → (start=total-20, end=total-1)
		→ folder.FetchAsync(start, end, UniqueId|Envelope|InternalDate)
		→ .OrderByDescending(s => s.Index)
		→ Select(s => new EmailItem { UniqueId, From, Subject, Date })
Крок 7: lstMessages.ItemsSource = items → ListView відображає 20 листів
```

**Файли:** `MainWindow.MainWindow_Loaded` → `ImapService.GetAllSelectableFoldersAsync` → `ImapService.GetPageAsync`

---

### 3.3 Читання повного листа

```
Крок 1: Користувач обирає лист у lstMessages → натискає "Open"
Крок 2: MainWindow.BtnOpen_Click
		→ lstMessages.SelectedItem as EmailItem → item.UniqueId
Крок 3: await _imap.GetMessageAsync(_currentFolder, item.UniqueId)
		→ folder.GetMessageAsync(uid) → повний MimeMessage із сервера
Крок 4: new ViewMessageWindow(_imap, _currentFolder, msg, item.UniqueId)
		→ конструктор: txtFrom/txtTo/txtSubject/txtDate.Text заповнюються
		→ txtBody.Text = msg.TextBody ?? msg.HtmlBody ?? "(no text)"
Крок 5: view.ShowDialog() → модальне вікно (Owner = MainWindow)
```

**Файли:** `MainWindow.BtnOpen_Click` → `ImapService.GetMessageAsync` → `ViewMessageWindow` конструктор

---

### 3.4 Надсилання листа (з вкладенням)

```
Крок 1: MainWindow.BtnNew_Click → new ComposeWindow() → ShowDialog()
Крок 2: Користувач натискає "Attach files"
		→ ComposeWindow.BtnAttach_Click
		→ OpenFileDialog (Multiselect=true)
		→ _attachments.Add(file) для кожного обраного файлу
Крок 3: Користувач заповнює To, Subject, Body → натискає "Send"
Крок 4: ComposeWindow.BtnSend_Click
		→ from = SessionState.Email; password = SessionState.Password
		→ валідація: перевірка на порожні from/password і to
		→ new MimeMessage()
		→ message.From.Add(MailboxAddress.Parse(from))
		→ message.To.Add(MailboxAddress.Parse(to))
		→ new BodyBuilder { TextBody = txtBody.Text }
		→ foreach attachment: builder.Attachments.Add(path)
		→ message.Body = builder.ToMessageBody()
		→ await SmtpService.SendAsync(from, password, message)
Крок 5: SmtpService.SendAsync (static)
		→ using var smtp = new SmtpClient()
		→ smtp.ConnectAsync("smtp.gmail.com", 587, StartTls)
		→ smtp.AuthenticationMechanisms.Remove("XOAUTH2")
		→ smtp.AuthenticateAsync(fromEmail, password)
		→ smtp.SendAsync(message)
		→ smtp.DisconnectAsync(true)
Крок 6: MessageBox "Message sent." → this.Close()
```

**Файли:** `ComposeWindow.BtnSend_Click` → `SmtpService.SendAsync`

---

### 3.5 Видалення листа

```
Крок 1: Користувач обирає лист → натискає "Delete"
Крок 2: MainWindow.BtnDelete_Click
		→ lstMessages.SelectedItem as EmailItem → item.UniqueId
Крок 3: await _imap.DeleteAsync(_currentFolder, item.UniqueId)
		→ folder.OpenAsync(FolderAccess.ReadWrite)
		→ folder.AddFlagsAsync(uid, MessageFlags.Deleted, silent:true)
		→ folder.ExpungeAsync()  ← фізично видаляє з сервера
		→ folder.CloseAsync(expunge:true)
		→ folder.OpenAsync(FolderAccess.ReadOnly) ← відновлює стан
Крок 4: await LoadPageAsync() → оновлює список листів
```

**Файли:** `MainWindow.BtnDelete_Click` → `ImapService.DeleteAsync` → `MainWindow.LoadPageAsync`

---

## 4. ЧОМУ ЦЕ ЗРОБЛЕНО САМЕ ТАК

### 4.1 ImapService як instance-клас (IAsyncDisposable) vs SmtpService як static-клас

**а) ПРОБЛЕМА:**  
IMAP — це stateful протокол із постійним з'єднанням. Папки залишаються відкритими між запитами; клієнт зберігає авторизацію протягом всього сеансу. Якщо створювати новий IMAP-клієнт на кожну операцію — отримаємо значні затримки (TCP handshake + TLS + AUTHENTICATE = ~1–2 секунди кожного разу).

**б) РІШЕННЯ:**  
`ImapService` є звичайним `class` (instance), реалізує `IAsyncDisposable`. Один екземпляр створюється в `LoginWindow.BtnLogin_Click` і передається в `MainWindow` через конструктор. З'єднання живе весь сеанс.  
`SmtpService` — `static class` із методом `SendAsync`, бо SMTP — транзакційний протокол: підключився → надіслав → відключився. Постійне з'єднання там не потрібне.

**в) ЧОМУ ПРАВИЛЬНО:**  
Це відповідає природі протоколів: IMAP розрахований на persistent connections (RFC 3501), SMTP — на short-lived transactions (RFC 5321). Тримати IMAP-клієнт alive — стандартна практика в усіх поштових клієнтах (Thunderbird, Outlook).

**г) АЛЬТЕРНАТИВА:**  
Можна зробити обидва сервіси static і підключатися/відключатися на кожну операцію.

**д) ПОРІВНЯННЯ:**  
| Поточний підхід | Альтернатива |
|---|---|
| IMAP: одне підключення — швидкі операції | IMAP: re-connect щоразу — ~1-2 с затримки |
| SMTP: нове підключення на кожен лист — коректно для транзакцій | SMTP: постійне з'єднання — ресурси займаються без потреби |
| Потребує явного `DisposeAsync` | Не потребує управління ресурсами |

---

### 4.2 SessionState як статичний клас для передачі облікових даних між вікнами

**а) ПРОБЛЕМА:**  
`ComposeWindow` потребує `email` і `password` для відправки листа, але відкривається з `MainWindow.BtnNew_Click` без параметрів. Передавати їх через конструктор кожного вікна вручну — громіздко.

**б) РІШЕННЯ:**  
`SessionState` (`Mail_Manager/Models/SessionState.cs`) — статичний клас із властивостями `Email` і `Password`. Встановлюється одноразово в `LoginWindow.BtnLogin_Click` після успішної автентифікації.

**в) ЧОМУ ПРАВИЛЬНО (в навчальному контексті):**  
Просто, зрозуміло, не потребує DI-контейнера. Для одного активного сеансу (одного користувача за раз) підходить ідеально.

**г) АЛЬТЕРНАТИВА:**  
Dependency Injection (`IServiceProvider`) з реєстрацією `SessionState` як `Singleton` у DI-контейнері (наприклад, через `Microsoft.Extensions.DependencyInjection`).

**д) ПОРІВНЯННЯ:**  
| Поточний (static class) | DI Singleton |
|---|---|
| Простий, нульовий boilerplate | Потребує конфігурації DI-контейнера |
| Важко тестувати (global state) | Легко мокувати у тестах |
| Потокобезпека не гарантована | Можна зробити thread-safe |
| Ідеально для одного користувача | Потрібний для multi-user або тестів |

---

### 4.3 Пагінація через index-based FetchAsync

**а) ПРОБЛЕМА:**  
Gmail-скринька може містити тисячі листів. Завантажити всі одночасно (метадані + тіло) — нереально ні за часом, ні за пам'яттю.

**б) РІШЕННЯ:**  
`ImapService.GetPageRange` (`ImapService.cs`, рядки 109–117) обчислює `start`/`end` індекси в масиві папки для конкретної сторінки. `GetPageAsync` завантажує лише `MessageSummaryItems.UniqueId | Envelope | InternalDate` (без тіла листа!). Сортує за `Index` DESC, щоб нові листи були першими.

**в) ЧОМУ ПРАВИЛЬНО:**  
Fetch лише summary (envelope без body) — стандартна IMAP-практика (UID FETCH). Передача лише потрібних полів мінімізує трафік і час відповіді.

**г) АЛЬТЕРНАТИВА:**  
IMAP SEARCH + UID SORT із серверним сортуванням (якщо сервер підтримує SORT extension).

**д) ПОРІВНЯННЯ:**  
| Поточний (index-based) | SEARCH/SORT |
|---|---|
| Простий, не потребує server extension | Потребує перевірки `CAPABILITY SORT` |
| Може мати off-by-one при паралельному видаленні | Завжди консистентний результат |
| Достатньо для навчального проєкту | Рекомендовано для production |

---

### 4.4 EF Core Code-First + SQL Server для зберігання користувачів

**а) ПРОБЛЕМА:**  
Потрібно зберігати список дозволених користувачів (email + пароль) і перевіряти їх при логіні.

**б) РІШЕННЯ:**  
`MailDbContext` (`Mail_Data_Access/MailDbContext.cs`) наслідує `DbContext`. Таблиця `Users` описана через `DbSet<User>`. Seed-data додана через `OnModelCreating`. Міграція `20251112151453_init` створює схему.

**в) ЧОМУ ПРАВИЛЬНО:**  
Code-First дозволяє описати схему БД як C#-класи і керувати змінами через міграції — це стандарт у .NET-екосистемі. Надалі легко додати нові поля або таблиці.

**г) АЛЬТЕРНАТИВА:**  
SQLite з `Microsoft.EntityFrameworkCore.Sqlite` (локальний файл без потреби у сервері).

**д) ПОРІВНЯННЯ:**  
| SQL Server (Somee) | SQLite |
|---|---|
| Хмарна БД, доступна з будь-якого ПК | Локальний файл, не потрібен сервер |
| Потребує мережевого з'єднання | Офлайн-доступ |
| Підходить для командної роботи | Ідеально для одного розробника |
| Реальний досвід із хмарною БД | Простіше розгортати |

---

### 4.5 Видалення листа через AddFlags + Expunge

**а) ПРОБЛЕМА:**  
IMAP не видаляє листи одразу. Видалення — двоетапний процес: спочатку позначити прапором `\Deleted`, потім виконати `EXPUNGE`.

**б) РІШЕННЯ:**  
`ImapService.DeleteAsync` (`ImapService.cs`, рядки 160–174): відкриває папку в `ReadWrite`, викликає `AddFlagsAsync(uid, MessageFlags.Deleted, true)`, потім `ExpungeAsync()`, закриває та знову відкриває в `ReadOnly`.

**в) ЧОМУ ПРАВИЛЬНО:**  
Це єдиний коректний спосіб видалення за RFC 3501. `Expunge` гарантує, що повідомлення вилучено з серверу, а не просто позначено.

**г) АЛЬТЕРНАТИВА:**  
Переміщення листа до папки `[Gmail]/Trash` через `MoveToAsync` (більш "м'яке" видалення, лист потрапляє у кошик).

**д) ПОРІВНЯННЯ:**  
| Поточний (Expunge) | MoveToAsync (Trash) |
|---|---|
| Необоротно видаляє з сервера | Відновлюваний (через кошик) |
| Простіший код | Потрібно знати точний шлях до Trash |
| Відповідає очікуванню кнопки "Delete" | Більш user-friendly |

---

## 5. НАВЧАЛЬНІ КОМПРОМІСИ І ЩО Я БИ ЗРОБИВ В ПРОДАКШН

1. **Hard-coded IMAP/SMTP серверів.**  
   Я знаю, що `"imap.gmail.com"` і `"smtp.gmail.com"` вшиті в код (`ImapService.cs`, `SmtpService.cs`) — це спрощення. В продакшн-проєкті я б читав хост, порт і протокол із конфігураційного файлу (`appsettings.json` або `user secrets`), щоб підтримувати будь-який поштовий провайдер.

2. **Пароль у відкритому вигляді в БД.**  
   Я знаю, що `Password` зберігається у таблиці `Users` як plain-text — це критична вразливість. В продакшн-проєкті я б використав хешування через `BCrypt` або `Argon2`, тому що навіть якщо БД буде зламана, паролі залишаться захищеними.

3. **Пароль у `SessionState` в оперативній пам'яті.**  
   Я знаю, що зберігати Gmail App Password у статичній змінній — небезпечно (атаки типу memory dump). В продакшн-проєкті я б використав `System.Security.SecureString` або рефрешив OAUTH2-токен замість пароля.

4. **Credentials у конструкторі `LoginWindow`.**  
   Я знаю, що попереднє заповнення полів реальними даними (`Email.Text = "[EMAIL]"`) — це зручно для розробки, але неприйнятно для будь-якого оточення поза локальним. В продакшн я б це видалив і налаштував тестові дані через environment variables.

5. **Hard-coded connection string у `MailDbContext.OnConfiguring`.**  
   Я знаю, що вшивати рядок підключення до хмарної БД прямо в код — це порушення принципу конфіденційності та ускладнює розгортання. В продакшн-проєкті я б передавав `DbContextOptions` через DI-контейнер і зберігав рядок підключення в `appsettings.json` з Azure Key Vault або `dotnet user-secrets`.

6. **Відсутність MVVM (ViewModels).**  
   Я знаю, що весь UI-код у code-behind (`*.xaml.cs`) без ViewModel порушує принцип Separation of Concerns. В продакшн-проєкті я б реалізував MVVM (наприклад, через CommunityToolkit.Mvvm) — це спрощує тестування UI-логіки та масштабування.

7. **Відсутність обробки пагінації при виході за межі.**  
   `BtnNext_Click` не перевіряє, чи є ще листи — `_pageIndex` може вирости до значення, де `GetPageRange` поверне `(0, -1)`, і список стане пустим без повідомлення. В продакшн я б відключав кнопку "Next" коли сторінка порожня.

8. **Відсутність юніт-тестів.**  
   Я знаю, що проєкт не містить жодного тесту. В продакшн-проєкті я б написав юніт-тести для `GetPageRange` (критична логіка), `SmtpService.SendAsync` (через мок-клієнт) та валідаційної логіки `ComposeWindow`.

9. **`MailDbContext` без `using` у `LoginWindow`.**  
   `db = new MailDbContext()` у конструкторі вікна ніколи явно не dispose-ується. В продакшн я б реєстрував його через DI (`AddDbContext`) або використовував `using`.

10. **Відсутність асинхронного рекурсивного завантаження папок з progress-індикатором.**  
	`GetAllSelectableFoldersAsync` блокує UI-потік на час завантаження (все ж асинхронно, але без індикатора). В продакшн я б додав `ProgressBar` або `CancellationTokenSource` з кнопкою "Cancel".

---

## 6. ГЛОСАРІЙ ТЕРМІНІВ

| Термін | Пояснення |
|---|---|
| **IMAP** (Internet Message Access Protocol) | Протокол для читання пошти з сервера з можливістю роботи з папками; листи зберігаються на сервері |
| **SMTP** (Simple Mail Transfer Protocol) | Протокол для відправки email-повідомлень |
| **SSL/TLS** | Протоколи шифрування транспортного рівня; SSL застарів, TLS — поточний стандарт |
| **STARTTLS** | Команда підвищення незашифрованого з'єднання до TLS-зашифрованого (використовується SMTP на порту 587) |
| **SslOnConnect** | Режим MailKit: TLS встановлюється одразу при підключенні (IMAP port 993) |
| **UID** (UniqueId) | Унікальний числовий ідентифікатор листа в IMAP-папці; стабільний навіть після сортування |
| **Expunge** | IMAP-команда, що фізично видаляє листи з прапором `\Deleted` |
| **MimeMessage** | Клас MimeKit, що представляє повне RFC 2822/MIME email-повідомлення |
| **BodyBuilder** | Клас MimeKit для зручного конструювання тіла листа (текст + HTML + вкладення) |
| **Envelope** | IMAP-термін: метадані листа (From, To, Subject, Date) без тіла — швидко отримується через FETCH |
| **FolderAttributes.NoSelect** | Прапор IMAP-папки: папка існує у дереві, але не може містити листи (лише підпапки) |
| **ImapClient** | Основний клас MailKit для роботи з IMAP-сервером |
| **IMailFolder** | Інтерфейс MailKit, що представляє IMAP-папку |
| **PersonalNamespaces** | Колекція IMAP namespace-ів для особистих папок (RFC 2342) |
| **IAsyncDisposable** | Інтерфейс .NET для асинхронного звільнення ресурсів (`await using`) |
| **DbSet\<T\>** | Клас EF Core, що представляє таблицю БД та надає LINQ-запити до неї |
| **Migration** | Файл EF Core, що описує зміни схеми БД у вигляді коду; дозволяє відтворити схему з нуля |
| **Seed Data** | Початкові дані, що автоматично вставляються в таблицю при застосуванні міграції |
| **Code-Behind** | Клас C# (`*.xaml.cs`), пов'язаний із XAML-файлом; містить обробники подій UI |
| **XOAUTH2** | Механізм авторизації Google через OAuth2 токени; видаляється зі списку, щоб примусово використати Basic Auth (App Password) |
| **App Password** | 16-символьний пароль, що генерується Google для доступу до пошти через IMAP/SMTP без основного пароля акаунту |
| **MaterialDesignThemes** | WPF-бібліотека стилів, що реалізує Google Material Design |
| **BundledTheme** | Клас MaterialDesignThemes для декларативного вибору кольорової схеми в XAML |
| **TFM** (Target Framework Moniker) | Ідентифікатор цільового фреймворку в .csproj (`net8.0-windows`, `net8.0`) |
| **DI** (Dependency Injection) | Патерн інверсії залежностей; передача залежностей зовні замість їх створення всередині |
| **DTO** (Data Transfer Object) | Об'єкт, призначений виключно для передачі даних між шарами (наприклад, `EmailItem`) |

---

## 7. ЙМОВІРНІ ПИТАННЯ НА ІНТЕРВ'Ю

### П1: Чому `ImapService` є instance-класом, а `SmtpService` — static?

**Відповідь:**  
Я зробив `ImapService` instance-класом, тому що IMAP — це stateful протокол із постійним з'єднанням (RFC 3501). Тримати один відкритий `ImapClient` протягом усього сеансу значно ефективніше, ніж підключатися на кожну операцію. Я реалізував `IAsyncDisposable` у `ImapService` (`ImapService.cs`, метод `DisposeAsync`), щоб гарантувати коректне закриття TCP-з'єднання при завершенні роботи з вікном.  
`SmtpService` — статичний, бо SMTP — транзакційний: підключився, надіслав, відключився. Постійне з'єднання там немає сенсу. Я усвідомлюю, що в продакшн-проєкті `SmtpService` також варто зробити instance-класом із передачею через DI, щоб його можна було мокувати у тестах.

---

### П2: Як реалізована пагінація листів і чому саме так?

**Відповідь:**  
Я реалізував пагінацію через index-based `FetchAsync` у `ImapService.GetPageAsync` (`ImapService.cs`). Метод `GetPageRange` обчислює `start` і `end` індекси від кінця масиву папки, бо листи з великим індексом — новіші. Завантажується лише `MessageSummaryItems.UniqueId | Envelope | InternalDate` — без тіла листа, що мінімізує трафік. Я обрав `pageSize = 20` (`MainWindow.cs`) як розумний баланс між кількістю запитів і швидкістю завантаження. Я усвідомлюю, що поточна реалізація може показати пусту сторінку при виході за межі — в продакшн я б додав перевірку та відключав кнопку "Next".

---

### П3: Як відбувається автентифікація і де зберігаються облікові дані?

**Відповідь:**  
Авторизація двоетапна. Спочатку я перевіряю пару email+password у базі даних SQL Server через EF Core (`LoginWindow.BtnLogin_Click` → `db.Users.Where(...).FirstOrDefault()`). Якщо користувач знайдений, я намагаюся підключитися до Gmail IMAP (`ImapService.ConnectAsync`). Якщо IMAP-автентифікація успішна, записую дані у `SessionState.Email` і `SessionState.Password` (`SessionState.cs`). Я знаю, що зберігати пароль у статичній змінній у пам'яті — це спрощення; в продакшн я б використав OAuth2 з `refresh_token` замість App Password.

---

### П4: Чому ти видаляєш `XOAUTH2` з `AuthenticationMechanisms`?

**Відповідь:**  
Я видаляю `XOAUTH2` і в `ImapService.ConnectAsync`, і в `SmtpService.SendAsync` тому, що MailKit за замовчуванням намагається використати OAuth2, якщо сервер його підтримує. Google пропонує XOAUTH2, але для його роботи потрібен Bearer-токен. Мій додаток використовує Gmail App Password (Basic Auth), тому без видалення XOAUTH2 MailKit намагався б OAuth2 і отримував помилку. Це тимчасове рішення для навчального проєкту; в продакшн я б реалізував повний OAuth2 flow через Google Identity Services.

---

### П5: Як побудована взаємодія між вікнами? Чому не MVVM?

**Відповідь:**  
Я побудував взаємодію через пряму передачу залежностей у конструктори вікон: `LoginWindow` передає `ImapService` у `MainWindow`, `MainWindow` передає `ImapService`, `IMailFolder`, `MimeMessage` і `UniqueId` у `ViewMessageWindow`. `ComposeWindow` отримує `to` і `subject` як опціональні параметри конструктора для підтримки Reply. Я свідомо не реалізував MVVM, тому що для навчального проєкту такого масштабу це зайве ускладнення. Я знаю, що MVVM покращує тестованість і Separation of Concerns — в продакшн-проєкті я б обрав CommunityToolkit.Mvvm.

---

### П6: Що робить `GetAllSelectableFoldersAsync` і чому там рекурсія?

**Відповідь:**  
Я реалізував рекурсивний обхід (`RecurseAsync` всередині `GetAllSelectableFoldersAsync`, `ImapService.cs`) тому, що структура IMAP-папок — це дерево довільної глибини. Gmail має папки на кількох рівнях: `[Gmail]/Sent Mail`, `[Gmail]/Drafts` тощо. Я перевіряю `FolderAttributes.NoSelect`, щоб не додавати вузлові папки, в яких немає листів. В кінці роблю `DistinctBy(f => f.FullName)`, щоб уникнути дублів (Inbox може зустрітися двічі). Я усвідомлюю, що для дуже великих поштових скриньок це може бути повільно — в продакшн я б додав індикатор прогресу та `CancellationToken` з таймаутом.

---

### П7: Як реалізована відправка листа з вкладеннями?

**Відповідь:**  
Я використовую MimeKit `BodyBuilder` у `ComposeWindow.BtnSend_Click` (`ComposeWindow.xaml.cs`). `BodyBuilder` дозволяє задати `TextBody` і потім додати вкладення через `builder.Attachments.Add(path)` — бібліотека сама визначає MIME-тип і кодує файл у Base64. Список шляхів до файлів я зберігаю у `_attachments` (`List<string>`), який наповнюється через `OpenFileDialog` в `BtnAttach_Click`. Я усвідомлюю, що поточний код не відображає список доданих файлів у UI (лише `MessageBox` з кількістю) — в продакшн я б показував їх у окремому `ListBox`.

---

### П8: Що таке EF Core migration і навіщо вона тут?

**Відповідь:**  
EF Core migration — це C#-клас, що описує дельту змін схеми БД. Я створив міграцію `20251112151453_init` (`Mail_Data_Access/Migrations/`), яка описує створення таблиці `Users` та вставку seed-запису. Це дозволяє відтворити схему БД на будь-якому сервері командою `dotnet ef database update`, без ручного написання SQL. Я знаю, що seed-дані з паролем у міграції — це навчальний компроміс; в продакшн seed-дані не повинні містити реальних облікових даних.

---

### П9: Чому `MailDbContext` підключається до хмарного SQL Server (Somee)?

**Відповідь:**  
Я використовую хмарний SQL Server на Somee, щоб БД була доступна всім учасникам команди без необхідності розгортати локальний сервер. Рядок підключення захардкожений у `MailDbContext.OnConfiguring` (`MailDbContext.cs`) — я залишив також закоментований варіант з `(localdb)\MSSQLLocalDB` для локальної розробки. Я усвідомлюю, що вшивати рядок підключення в код — це порушення безпеки. В продакшн-проєкті я б виніс його в `appsettings.json` + `dotnet user-secrets` або Azure Key Vault, а сам `DbContext` реєстрував через DI-контейнер.

---

### П10: Що відбувається якщо IMAP-з'єднання перервалося посередині сеансу?

**Відповідь:**  
У поточній реалізації немає автоматичного перепідключення (reconnect logic). Якщо з'єднання перерветься, наступна операція (наприклад, `GetPageAsync` або `DeleteAsync`) кине виняток `ImapProtocolException` або `IOException`, який я перехоплюю в `catch`-блоках головного вікна (`MainWindow.LoadPageAsync`, `BtnOpen_Click` тощо) і показую `MessageBox` із повідомленням про помилку. Я усвідомлюю, що це не user-friendly. В продакшн-проєкті я б додав у `ImapService` метод `EnsureConnectedAsync`, який при виявленні `!_client.IsConnected` виконує `ConnectAsync` знову, і лише після цього повторює операцію.

---

*Документ згенеровано на основі вихідного коду рішення `Mail_Manager.sln`. Усі реальні облікові дані замінено на плейсхолдери відповідно до вимог безпеки.*

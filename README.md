# 🏗 GeotekMetallComplete — WPF приложение для управления строительными проектами

---

## 🚀 О проекте

**GeotekMetallComplete** — это WPF-приложение для автоматизации работы
строительной компании, позволяющее пользователю:
- 👥 Входить в систему под одной из трёх ролей (администратор, бухгалтер, работник)
- 📝 Создавать и обрабатывать заявки на строительные работы
- 🏗 Управлять проектами, этапами и задачами с автоматическим расчётом статуса
- 💰 Вести бюджет, транзакции и финансовые отчёты по проектам
- 📄 Загружать, просматривать, редактировать и печатать документы (Word, PDF)
- 📷 Отправлять фотоотчёты по задачам (до 10 фото, защита от дублей)
- 👤 Редактировать свой профиль

---

## 🖼 Скриншоты

<details>
<summary><b>🔐 Окно авторизации</b></summary>

<img src="docs/auth.PNG" alt="Authorization window" width="700">

</details>

<details>
<summary><b>👑 Окно администратора</b></summary>

<img src="docs/admin.PNG" alt="Admin window" width="900">

</details>

<details>
<summary><b>💰 Окно бухгалтера</b></summary>

<img src="docs/accountant.PNG" alt="Accountant window" width="900">

</details>

<details>
<summary><b>👷 Окно работника</b></summary>

<img src="docs/worker.PNG" alt="Worker window" width="900">

</details>

<details>
<summary><b>📋 Управление пользователями</b></summary>

<img src="docs/users.PNG" alt="User management" width="900">

</details>

<details>
<summary><b>📨 Управление заявками</b></summary>

<img src="docs/requests.PNG" alt="Requests management" width="900">

</details>

<details>
<summary><b>🏗 Управление проектами и этапами</b></summary>

<img src="docs/projects.PNG" alt="Projects management" width="900">

</details>

<details>
<summary><b>💵 Бюджет и транзакции</b></summary>

<img src="docs/budget.PNG" alt="Budget page" width="900">

</details>

<details>
<summary><b>📑 Aкты выполненных работ</b></summary>

<img src="docs/contracts.PNG" alt="Contracts page" width="900">

</details>

<details>
<summary><b>📊 Финансовые отчёты</b></summary>

<img src="docs/finance.PNG" alt="Finance reports" width="900">

</details>

<details>
<summary><b>✏️ Редактор документов</b></summary>

<img src="docs/editor.PNG" alt="Document editor" width="900">

</details>

<details>
<summary><b>👤 Мои данные</b></summary>

<img src="docs/profile.PNG" alt="Profile page" width="700">

</details>

---

## 🛠 Технологии

<details>
<summary><b>Показать полный стек</b></summary>

| Слой | Технология |
|------|------------|
| UI | WPF (.NET Framework 4.8) |
| ORM | Entity Framework 6 |
| БД | SQL Server |
| Документы | OpenXML, DocX (Xceed), iTextSharp, PdfPig |
| Шифрование | AES + SHA-256 |
| Изображения | WPF Imaging (JpegBitmapEncoder, BitmapImage) |

</details>

---

## 🏗 Архитектура

<details>
<summary><b>Кратко о структуре</b></summary>

Приложение построено по классической WPF-архитектуре с разделением
на окна-оболочки, страницы и модели данных:

- **Windows** — окна ролей (AdminWindow, AccountantWindow, WorkerWindow)
- **Pages** — страницы для каждой функциональной области
- **Models** — 16 сущностей
- **Services** — утилиты (шифрование, хелперы для placeholder)

</details>

<details>
<summary><b>Основное</b></summary>

| Класс | Назначение |
|-------|-----------|
| Authorization | Авторизация с проверкой роли |
| AdminWindow / AccountantWindow / WorkerWindow | Окна для ролей |
| userManagementPage | CRUD пользователей и ролей |
| requestManagementPage | Обработка заявок, создание проектов |
| ProjectsManagementPage | Управление проектами, этапами, задачами |
| BudgetPage | Транзакции с прикреплением файлов |
| ContractsPage / ActsOfWorkPage | Договоры и акты выполненных работ |
| FinanceManagementPage | Финансовые отчёты по проектам |
| DocumentEditorWindow | Создание и редактирование .docx / .pdf |
| viewingDocument | Просмотр и печать документов |
| General | AES-шифрование + хелперы для TextBox |

</details>

---

## 🔐 Роли и возможности

<details>
<summary><b>Администратор</b></summary>

- Управление пользователями (создание, редактирование, удаление)
- Обработка заявок клиентов (одобрение / отклонение)
- Создание проектов с назначением менеджера
- Управление клиентами, проектами, финансовыми отчётами
- Просмотр деталей проектов, задач и отчётов

</details>

<details>
<summary><b>Бухгалтер</b></summary>

- Финансовые отчёты по проектам
- Управление бюджетом (транзакции с прикреплением документов)
- Акты выполненных работ
- Загрузка, просмотр, редактирование, печать документов

</details>

<details>
<summary><b>Работник</b></summary>

- Просмотр своих задач с фильтрацией по проекту и статусу
- Просмотр деталей проектов
- Отправка фотоотчётов по задачам (до 10 фото)
- Редактирование профиля

</details>

---

## ⚙️ Установка

<details>
<summary><b>Требования</b></summary>

- Windows 10/11
- Visual Studio 2019/2022
- .NET Framework 4.8
- SQL Server (LocalDB или полноценный сервер)
- NuGet-пакеты: EntityFramework, DocumentFormat.OpenXml, Xceed.Words.NET, iTextSharp, PdfPig

</details>

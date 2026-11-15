# Мои заметки для защиты EasyShop KZ

Преподавателю отправляй `../for_teacher/EasyShop_KZ_Defense_EN.pptx`. Этот файл — для твоей подготовки: пояснения, порядок демонстрации, команды и ответы на вопросы. В презентации 12 слайдов; она на английском, эти заметки — на русском с короткими английскими фразами для выступления.

## Перед отправкой

1. Впиши настоящее полное ФИО на первом слайде. Если работа индивидуальная — удали строку второго участника.
2. Просмотри презентацию в PowerPoint, особенно скриншоты и таблицу сравнения технологий.
3. Проверь требования преподавателя к отправке исходников: презентация и исходный проект — разные материалы. Если нужен проект, отправь исходники и README; зависимости node_modules, bin/obj и временные artifacts в архив не включай.
4. Прочитай код и убедись, что можешь объяснить каждую заявленную функцию. Личное участие и устная защита — отдельные требования курса.

## Что говорить по слайдам

### 1. EasyShop KZ — 30 секунд

**English:** “EasyShop KZ is an online store built with C# and ASP.NET Core Blazor. It supports product discovery, a shopping cart, customer accounts, checkout and store administration.”

Представь себя и тему. Не перечисляй все файлы проекта.

### 2. Purpose and practical value — 1 минута

**English:** “The goal is to provide a clear shopping workflow for customers and consistent inventory management for administrators.”

Объясни две роли: покупатель выбирает и заказывает, администратор управляет каталогом и заказами. Практическая польза — единый учёт остатков и сохранённая история.

### 3. Technology comparison and stack selection — 1–2 минуты

**English:** “Blazor lets us implement interactive UI logic in C#. SQLite simplifies local setup. These choices fit the scope of a university project.”

Blazor использует постоянное соединение с сервером. React потребовал бы отдельного клиентского стека и API. SQLite — база в файле, удобная для демонстрации; она не заменяет серверную СУБД при больших нагрузках. Сравнение объясняет выбор технологий, а не доказывает, что один стек всегда лучше остальных.

### 4. Development plan and methodology — 1 минута

**English:** “The workflow starts with roles and a data model, then moves to screens, business rules and verification.”

Объясни порядок разработки: сценарии → данные → интерфейс → сервисы → проверки. Не приписывай себе действия, которые не выполнял: говори о структуре и проверяемых результатах проекта.

### 5. Application architecture — 2 минуты

**English:** “Razor components handle presentation. ShopService coordinates business operations. EF Core persists data in SQLite, while ShopRules contains pure functions.”

Основная цепочка: страница вызывает ShopService, сервис создаёт DbContext, EF Core читает или изменяет SQLite. CartState отвечает за корзину. Authentication передаёт сведения о пользователе и роли.

Файлы, которые стоит уметь открыть:

- `src/EasyShop.Web/Program.cs` — DI, cookies, endpoints входа и регистрации, запуск.
- `src/EasyShop.Web/Services/ShopService.cs` — checkout, CRUD и переходы статусов.
- `src/EasyShop.Web/Data/ShopDbContext.cs` — таблицы и связи.
- `src/EasyShop.Web/Functional/ShopRules.cs` — чистые правила.

### 6. State, reusable components and FP — 1–2 минуты

**English:** “ProductCard is reused across pages. CartState manages the cart and protected session storage. Pure functions return updated data without changing the original collection.”

`Item` — параметр ProductCard. `Changed` — уведомление об изменениях корзины. Подписки снимаются в Dispose. `AuthenticationState` — каскадное состояние авторизации. FP проявляется в неизменяемых обновлениях корзины, LINQ и чистых функциях ShopRules. Защищённое хранение корзины не отменяет серверную проверку цены и остатков.

### 7. User interface: product catalog — 1 минута

**English:** “Customers can search by name, filter by category and availability, sort by price and open product details.”

На скриншоте покажи поиск, категории, сортировку, цену и добавление товара. При живой демонстрации используй короткий запрос, например `mouse`.

### 8. Responsive layout: mobile viewport — 30–45 секунд

**English:** “The layout adapts to a narrow viewport using responsive grids and CSS media queries. Mobile behavior was checked through browser emulation.”

Честно уточни, что это эмуляция размера экрана, а не проверка на физическом телефоне.

### 9. User interface: admin dashboard — 1 минута

**English:** “Administrators can manage products and categories, review orders and update their status. Customer accounts cannot access these pages.”

Покажи, где находятся товары, категории и заказы. Роль Admin проверяется на маршрутах. Счётчик Customers в текущей реализации считает всех пользователей, включая администратора.

### 10. Order reliability and security — 2 минуты

**English:** “Checkout reloads prices from the database and updates stock inside a transaction. If any item cannot be reserved, the operation rolls back. Cancellation returns stock only once.”

Объясни на примере: в корзине два товара, второго не хватает — первый не должен остаться списанным. Условный UPDATE не даёт двум заказам купить одну последнюю единицу. При отмене сначала атомарно меняется статус, затем возвращается склад.

Пароль хранится как хеш. CSRF-токен защищает POST-формы. История фильтруется по ID текущего пользователя. Снимки названия и цены в строках заказа сохраняются даже при удалении товара.

### 11. Testing and verified results — 1 минута

**English:** “The recorded verification passed 69 .NET tests and six browser scenarios. Tests cover business rules, database operations, authentication, access control and customer/admin workflows.”

Результаты относятся к проверке от 15.11.2026. 6 браузерных сценариев — это 2 длинных сценария (покупатель и администратор) в 3 профилях: Edge desktop, Edge mobile и Firefox desktop. Это не 6 различных браузеров. Процент покрытия строк и ветвей не измерялся, 100% покрытия не заявляй.

### 12. Limitations and future development — 1 минута

**English:** “This is an educational store. Payments are simulated. Future work includes production migrations, precise monetary storage and testing at larger scale.”

Основные ограничения уже указаны на слайде: нет настоящего эквайринга, реальные устройства и другие ОС не проверялись, SQLite и загрузка всего каталога ограничивают масштаб. В исходной схеме денежные значения сохраняются через double; для финансового production нужна миграция к точным минимальным единицам. Не обещай готовность к реальным платежам.

После этого переходи к демонстрации или вопросам.

## Живая демонстрация — примерно 5 минут

1. Открой главную и каталог; найди `mouse`, выбери категорию и сортировку, затем Reset.
2. Открой товар, добавь в корзину, измени количество. Обнови страницу — корзина должна сохраниться в той же вкладке.
3. Войди заранее подготовленным покупателем. Покажи сохранённую корзину после входа.
4. Оформи заказ с Cash on Delivery и открой My Orders. Demo Card только имитирует способ оплаты.
5. В отдельном браузерном профиле войди администратором, открой новый заказ и переведи его в Processing.
6. Отмени заказ и покажи возврат остатков. На отдельном заказе можно показать Processing → Completed; завершённый заказ отменить нельзя.
7. Если останется время, добавь тестовую категорию и товар; покажи, что категорию с товаром удалить нельзя.

Для демонстрации заранее создай отдельные учётные записи. Пароли не добавляй в слайды и не отправляй преподавателю.

## Команды для себя

Запускай из папки `EasyShop_KZ`.

```powershell
dotnet test EasyShop.sln
dotnet run --project src/EasyShop.Web
```

Обычно адрес — `http://localhost:5138`; точный URL смотри в терминале. Остановка: Ctrl+C.

Администратор создаётся при запуске с переменными окружения. Подставь собственный пароль от 12 символов:

```powershell
$env:EASYSHOP_ADMIN_EMAIL="admin@easyshop.local"
$env:EASYSHOP_ADMIN_PASSWORD="<your-private-password>"
dotnet run --project src/EasyShop.Web
```

Если такой email уже существует, запуск не заменяет пароль существующего пользователя. Не используй реальные персональные данные в демонстрационных заказах.

Браузерные проверки:

```powershell
dotnet build EasyShop.sln
cd tests/e2e
npm.cmd ci
npx.cmd playwright install msedge firefox
npx.cmd playwright test
```

После успешного прогона ожидается 6 passed. Открыть HTML-отчёт можно командой `npx.cmd playwright show-report` из той же папки.

## Частые вопросы

| Вопрос | Короткий ответ |
|---|---|
| Что такое Blazor? | Фреймворк для веб-интерфейса на C#; здесь используется Interactive Server. |
| Что такое DI? | Сервис передаётся через контейнер зависимостей, а не создаётся внутри каждой страницы. |
| Зачем DbContextFactory? | Для каждой операции создаётся отдельный DbContext; один контекст не живёт весь Blazor circuit. |
| Где здесь FP? | ShopRules: чистые функции, LINQ и создание обновлённой коллекции вместо изменения исходной. |
| Что такое транзакция? | Набор изменений, которые фиксируются вместе или вместе откатываются. |
| Почему нельзя доверять цене в корзине? | Цена могла измениться; итог пересчитывается по актуальной базе. |
| Что предотвращает двойную отмену? | Атомарная проверка и смена прежнего статуса перед возвратом склада. |
| Как сохраняется история удалённого товара? | OrderLine хранит снимок названия, цены и количества без обязательной связи с Product. |
| Где хранится корзина? | В scoped CartState и ProtectedSessionStorage той же вкладки. |
| Как защищены чужие заказы? | Запрос ограничивается ID пользователя из проверенных authentication claims. |
| Какой процент покрытия? | Не измерялся. Известно количество пройденных тестов и конкретные сценарии. |
| Поддерживаются другие ОС? | Стек переносимый, но реально проверена Windows; проверки других ОС не выполнялись. |

## Если на демонстрации что-то не работает

- Не открывается магазин: проверь, что процесс запущен, и используй URL из терминала.
- Не видна админка: проверь email и роль аккаунта; обычная регистрация создаёт Customer.
- Ошибка CSRF: обнови страницу формы и повтори вход.
- Не хватает товара: уменьши количество или выбери другой товар; это штатная проверка.
- База занята: дождись завершения операции и повтори действие.
- Не удаляй базу ради исправления ошибки: в ней сохранены пользователи и заказы.

## Где лежат технические подробности

- [Полный отчёт проверок](../QA_REPORT_RU.md).
- [Сопоставление с 20 критериями](../REQUIREMENTS_RU.md).
- [Заметки разработчика](../DEVELOPER_GUIDE.md).
- [Исходный краткий план защиты](../DEFENSE_RU.md).

Общий лимит защиты по документу — 15–20 минут. Подстрой длительность рассказа под живую демонстрацию и оставь время на вопросы. Если участников двое, оба должны понимать весь проект.

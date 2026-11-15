"""Generate the editable defense slides. Requires python-pptx (pip install python-pptx)."""
from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'artifacts/presentation-tools'))
from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_SHAPE

prs=Presentation();prs.slide_width=Inches(13.333);prs.slide_height=Inches(7.5)
INK='192C2B';TEAL='1B594E';PALE='F1F6EF';MUTED='61716D'
def text(slide,x,y,w,h,value,size=24,color=INK,bold=False):
 box=slide.shapes.add_textbox(Inches(x),Inches(y),Inches(w),Inches(h));tf=box.text_frame;tf.word_wrap=True
 for i,line in enumerate(value.split('\n')):
  p=tf.paragraphs[0] if i==0 else tf.add_paragraph();p.text=line;p.font.name='Aptos';p.font.size=Pt(size);p.font.bold=bold;p.font.color.rgb=RGBColor.from_string(color);p.space_after=Pt(14)
 return box

def slide(title,subtitle=''):
 s=prs.slides.add_slide(prs.slide_layouts[6]);s.background.fill.solid();s.background.fill.fore_color.rgb=RGBColor.from_string(PALE)
 bar=s.shapes.add_shape(MSO_SHAPE.RECTANGLE,0,0,prs.slide_width,Inches(.14));bar.fill.solid();bar.fill.fore_color.rgb=RGBColor.from_string(TEAL);bar.line.fill.background()
 text(s,.65,.45,12,1,title,32,TEAL,True)
 if subtitle:text(s,.68,1.35,12,.7,subtitle,17,MUTED)
 text(s,.7,7.03,10,.3,'EasyShop KZ  /  C# · Blazor · .NET 10',11,MUTED);text(s,12,7.03,.7,.3,str(len(prs.slides)),11,MUTED)
 return s

def bullets(title,lines,subtitle=''):
 s=slide(title,subtitle);text(s,.85,2.0,11.7,4.8,'\n'.join('• '+x for x in lines),23);return s

s=slide('EasyShop KZ','Интернет-магазин на C# и ASP.NET Core Blazor')
text(s,.85,2.3,11.7,1.5,'От поиска товара до заказа\nс безопасным управлением складом',36,INK,True)
text(s,.85,4.6,11.7,1.3,'Участник 1: [впишите полное ФИО]\nУчастник 2: [впишите ФИО или удалите строку]',22,MUTED)
bullets('Цель и практическая значимость',['Цель: создать понятный интернет-магазин для покупателя и администратора.','Задачи: каталог, фильтры, корзина, учётные записи, заказы и управление товарами.','Практическая польза: единый учёт остатков и история заказов.','Учебный результат: применение C#, FP, Blazor, EF Core и автоматического тестирования.'])
s=slide('Анализ технологий и выбор стека','Сравнение подходов для учебного интернет-магазина')
rows=[['Подход','Сильная сторона','Цена выбора'],['Blazor Interactive Server','Интерактивный UI на C#; общая .NET среда','Постоянное соединение с сервером'],['Razor Pages / MVC','Простые серверные формы и SSR','Для интерактивных частей нужен JavaScript'],['React + ASP.NET API','Независимый клиент и сервер','Два стека и отдельный API-контракт'],['SQLite + EF Core','База в файле; простой запуск','Записи сериализуются; нужен план роста']]
table=s.shapes.add_table(5,3,Inches(.75),Inches(2),Inches(11.8),Inches(3.6)).table
for r,row in enumerate(rows):
 for c,value in enumerate(row):
  cell=table.cell(r,c);cell.text=value;cell.fill.solid();cell.fill.fore_color.rgb=RGBColor.from_string(TEAL if r==0 else 'FFFFFF')
  for p in cell.text_frame.paragraphs:p.font.size=Pt(18);p.font.color.rgb=RGBColor.from_string('FFFFFF' if r==0 else INK)
text(s,.85,5.95,11.6,.6,'Выбор: Blazor для интерфейса; обычные серверные формы для входа; SQLite для учебного масштаба.',20)
bullets('План и методология',['1. Выделить роли, сценарии и модель данных.','2. Построить страницы каталога, корзины, оформления и админки.','3. Вынести чистые правила и сервис работы с базой.','4. Проверить ошибки, одновременные изменения и права доступа.','5. Выполнить unit/integration/HTTP/E2E проверки и подготовить защиту.'])
s=slide('Архитектура приложения','Модульные границы и движение данных')
blocks=[(.8,2.25,'Razor Components','Pages · ProductCard · MainLayout'),(4.7,2.25,'ShopService','Валидация · транзакции · CRUD'),(8.6,2.25,'EF Core / SQLite','Users · Products · Orders'),(.8,4.5,'CartState','Scoped + protected sessionStorage'),(4.7,4.5,'ShopRules','Records · LINQ · чистые функции'),(8.6,4.5,'Authentication','Cookies · claims · роль Admin')]
for x,y,title,body in blocks:
 shape=s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE,Inches(x),Inches(y),Inches(3.6),Inches(1.5));shape.fill.solid();shape.fill.fore_color.rgb=RGBColor.from_string('FFFFFF');shape.line.color.rgb=RGBColor.from_string('D6E0DB');text(s,x+.18,y+.15,3.2,.45,title,21,TEAL,True);text(s,x+.18,y+.7,3.2,.6,body,16)
text(s,4.35,2.7,.35,.4,'→',24);text(s,8.25,2.7,.35,.4,'→',24)
bullets('Состояние, компоненты и FP',['ProductCard переиспользуется на главной и в каталоге через параметр Item.','AuthenticationState передаётся каскадно; роли ограничивают маршруты.','CartState публикует Changed и сохраняет корзину между обновлением и входом.','Изменения корзины сериализованы; компоненты снимают подписки при Dispose.','ShopRules не обращается к базе: расчёт суммы, фильтры, количества и статусы.'])
for name,title in [('edge-desktop-catalog.png','Интерфейс: каталог'),('edge-mobile-catalog.png','Адаптивность: мобильный экран'),('edge-desktop-dashboard.png','Интерфейс: панель администратора')]:
 image=ROOT/'artifacts'/name
 if image.exists():
  from PIL import Image
  width,height=Image.open(image).size
  s=slide(title,'Скриншот работающего приложения из Playwright')
  maxw,maxh=11.8,4.7;scale=min(maxw/width,maxh/height);w,h=width*scale,height*scale
  s.shapes.add_picture(str(image),Inches((13.333-w)/2),Inches(2.05),width=Inches(w),height=Inches(h))
bullets('Надёжность заказа и безопасность',['Сервер пересчитывает цены по базе, не доверяя данным корзины.','Условное списание склада выполняется в транзакции: нет частичного заказа.','Отмена сначала атомарно меняет статус, затем один раз возвращает остатки.','Снимки строк заказа сохраняются после удаления товара.','Пароли хешируются; формы защищены CSRF; история ограничена текущим пользователем.'])
bullets('Проверки и результаты',['69 тестов .NET: чистые правила, сервисы, SQLite и настоящие HTTP-формы.','Проверены гонки покупки последней единицы и одновременной отмены.','Playwright: покупатель и администратор; Edge desktop/mobile и Firefox.','Release-сборка с -warnaserror; аудит прямых и транзитивных NuGet-зависимостей.','Точные итоговые результаты и пути отчётов: docs/QA_REPORT_RU.md.'])
bullets('Ограничения и развитие',['Demo Card имитирует оплату; реального платёжного шлюза нет.','Мобильный viewport эмулируется; физические устройства, macOS/Linux и Safari не проверены.','Для роста: пагинация, серверная СУБД, профилирование и нагрузочные проверки.','Для production: миграции, резервные копии, rate limiting, управление ключами и точное хранение денежных сумм.'])
bullets('Сценарий защиты — 15–20 минут',['2 минуты: проблема, цель, технологии и сравнение.','4 минуты: архитектура, FP, компоненты и состояние.','5 минут: демонстрация покупки, истории и администрирования.','3 минуты: безопасность, транзакции, тесты и найденные ошибки.','Оставшееся время: ограничения, планы развития и вопросы каждому участнику.'])
output=ROOT/'docs/EasyShop_KZ_Defense.pptx';prs.save(output)
print(f'Created {output.name}: {len(prs.slides)} editable slides')

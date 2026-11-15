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


s=slide('EasyShop KZ','An online store built with C# and ASP.NET Core Blazor')
text(s,.85,2.3,11.7,1.5,'From product discovery to checkout\nwith reliable inventory management',36,INK,True)
text(s,.85,4.6,11.7,1.3,'Team member 1: [enter full name]\nTeam member 2: [enter full name or remove this line]',22,MUTED)
bullets('Purpose and practical value',[
 'Goal: build a clear, usable online store for customers and administrators.',
 'Objectives: product search, filters, cart, accounts, checkout and order management.',
 'Practical value: consistent inventory tracking and persistent order history.',
 'Learning outcome: apply C#, functional programming, Blazor, EF Core and automated testing.'
])
s=slide('Technology comparison and stack selection','Comparing approaches for a university online-store project')
rows=[['Approach','Strength','Trade-off'],
 ['Blazor Interactive Server','Interactive C# UI in a shared .NET stack','Requires a persistent server connection'],
 ['Razor Pages / MVC','Straightforward server forms and SSR','Interactive features require JavaScript'],
 ['React + ASP.NET API','Independent client and server','Two stacks and a separate API contract'],
 ['SQLite + EF Core','File-based database and simple setup','Serialized writes; limited scale']]
table=s.shapes.add_table(5,3,Inches(.75),Inches(2),Inches(11.8),Inches(3.6)).table
for r,row in enumerate(rows):
 for c,value in enumerate(row):
  cell=table.cell(r,c);cell.text=value;cell.fill.solid();cell.fill.fore_color.rgb=RGBColor.from_string(TEAL if r==0 else 'FFFFFF')
  for p in cell.text_frame.paragraphs:p.font.size=Pt(18);p.font.color.rgb=RGBColor.from_string('FFFFFF' if r==0 else INK)
text(s,.85,5.95,11.6,.8,'Selected stack: Blazor for the UI, standard server forms for authentication, and SQLite for the project scope.',20)
bullets('Development plan and methodology',[
 '1. Define user roles, workflows and the data model.',
 '2. Build the catalog, cart, checkout and administration screens.',
 '3. Separate pure business rules from database operations.',
 '4. Validate inputs, concurrent updates and access permissions.',
 '5. Verify workflows with unit, integration, HTTP and browser tests.'
])
s=slide('Application architecture','Module boundaries and the flow of data')
blocks=[(.8,2.25,'Razor Components','Pages · ProductCard · MainLayout'),
 (4.7,2.25,'ShopService','Validation · transactions · CRUD'),
 (8.6,2.25,'EF Core / SQLite','Users · Products · Orders'),
 (.8,4.5,'CartState','Scoped + protected sessionStorage'),
 (4.7,4.5,'ShopRules','Records · LINQ · pure functions'),
 (8.6,4.5,'Authentication','Cookies · claims · Admin role')]
for x,y,title,body in blocks:
 shape=s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE,Inches(x),Inches(y),Inches(3.6),Inches(1.5));shape.fill.solid();shape.fill.fore_color.rgb=RGBColor.from_string('FFFFFF');shape.line.color.rgb=RGBColor.from_string('D6E0DB');text(s,x+.18,y+.15,3.2,.45,title,21,TEAL,True);text(s,x+.18,y+.7,3.2,.6,body,16)
text(s,4.35,2.7,.35,.4,'→',24);text(s,8.25,2.7,.35,.4,'→',24)
bullets('State, reusable components and FP',[
 'ProductCard is reused on Home and Catalog through its Item parameter.',
 'AuthenticationState is cascading; roles restrict access to protected routes.',
 'CartState publishes Changed and preserves the cart across refresh and sign-in.',
 'Cart changes are serialized; components unsubscribe during disposal.',
 'ShopRules contains pure functions for totals, filters, quantities and status transitions.'
])
for name,title in [('edge-desktop-catalog.png','User interface: product catalog'),
 ('edge-mobile-catalog.png','Responsive layout: mobile viewport'),
 ('edge-desktop-dashboard.png','User interface: admin dashboard')]:
 image=ROOT/'artifacts'/name
 if image.exists():
  from PIL import Image
  width,height=Image.open(image).size
  s=slide(title,'Screenshot of the running application captured by Playwright')
  maxw,maxh=11.8,4.7;scale=min(maxw/width,maxh/height);w,h=width*scale,height*scale
  s.shapes.add_picture(str(image),Inches((13.333-w)/2),Inches(2.05),width=Inches(w),height=Inches(h))
bullets('Order reliability and security',[
 'The server reloads prices from the database rather than trusting cart values.',
 'Conditional inventory updates run in a transaction: failed orders roll back.',
 'Cancellation claims the status transition atomically before returning stock once.',
 'Order-line snapshots remain available after a product is deleted.',
 'Passwords are hashed; forms use CSRF protection; customers see only their orders.'
])
bullets('Testing and verified results',[
 '69 .NET tests passed: pure rules, services, SQLite and actual HTTP forms.',
 'Concurrency checks cover the final stock unit and simultaneous cancellation.',
 '6 browser scenarios passed: customer and admin in Edge desktop/mobile and Firefox.',
 'Release build passed without warnings; the dependency audit found no known vulnerable packages.',
 'The published application passed its Production startup smoke test.'
], 'Verified on 15 November 2026 · Windows · Edge and Firefox · mobile viewport emulation')
bullets('Limitations and future development',[
 'Demo Card simulates payment; there is no real payment gateway.',
 'Mobile tests use emulation. Physical devices, macOS/Linux and Safari were not tested.',
 'For larger workloads: pagination, a server database, profiling and load testing.',
 'For production: migrations, backups, rate limiting, key management and exact monetary storage.'
])
prs.core_properties.title='EasyShop KZ — Project Defense'
prs.core_properties.subject='C# and ASP.NET Core Blazor online-store project'
prs.core_properties.language='en-US'
output=ROOT/'docs/for_teacher/EasyShop_KZ_Defense_EN.pptx';output.parent.mkdir(parents=True,exist_ok=True);prs.save(output)
print(f'Created {output.name}: {len(prs.slides)} editable slides')

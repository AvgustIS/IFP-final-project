const {test,expect}=require('@playwright/test');
async function visit(page,path){await page.goto(path);await page.locator('main[data-ready="true"]').waitFor();}
async function login(page,email,password){await visit(page,'/login');await page.getByLabel('Email',{exact:true}).fill(email);await page.getByLabel('Password',{exact:true}).fill(password);await Promise.all([page.waitForURL(url=>url.pathname==='/'||url.searchParams.has('error')),page.getByRole('button',{name:'Sign in',exact:true}).click()]);await page.locator('main[data-ready="true"]').waitFor();}
async function noOverflow(page){expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1)).toBe(true);}

test('customer journey: filters, cart persistence, authentication, checkout and privacy',async({page,browser},testInfo)=>{
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await visit(page,'/');await expect(page.locator('.product-card')).toHaveCount(8);await noOverflow(page);
 await page.getByRole('link',{name:'Explore products'}).click();await expect(page.locator('.product-card')).toHaveCount(12);await page.screenshot({path:`../../artifacts/${testInfo.project.name}-catalog.png`,fullPage:true});
 await page.getByLabel('Search products').fill('mouse');await expect(page.locator('.product-card')).toHaveCount(1);
 await page.getByRole('button',{name:'Reset',exact:true}).click();await expect(page.locator('.product-card')).toHaveCount(12);
 await page.getByLabel('Category',{exact:true}).selectOption({label:'Stationery'});await expect(page.locator('.product-card')).toHaveCount(2);
 await page.getByRole('button',{name:'Reset',exact:true}).click();await page.getByLabel('Sort',{exact:true}).selectOption('low');
 await expect(page.locator('.product-card h3').first()).toHaveText('Gel Pen Pack');
 await page.getByLabel('In stock',{exact:true}).check();await expect(page.locator('.product-card')).toHaveCount(12);
 await page.getByLabel('Search products').fill('headphones');await expect(page.locator('.product-card')).toHaveCount(1);
 await page.locator('.product-card h3 a').click();await expect(page.getByRole('heading',{name:'Wireless Headphones'})).toBeVisible();
 await page.getByRole('button',{name:'Add to cart',exact:true}).click();await expect(page.locator('nav .count')).toHaveText('1');
 await visit(page,'/cart');await expect(page.locator('.cart-row')).toHaveCount(1);
 await page.getByRole('button',{name:'Increase',exact:true}).click();await expect(page.locator('.qty span')).toHaveText('2');
 await page.getByRole('button',{name:'Decrease',exact:true}).click();await expect(page.locator('.qty span')).toHaveText('1');
 await page.reload();await page.locator('main[data-ready="true"]').waitFor();await expect(page.locator('.cart-row')).toHaveCount(1);
 await page.getByRole('button',{name:'Remove',exact:true}).click();await expect(page.getByRole('heading',{name:'Your cart is empty'})).toBeVisible();
 await visit(page,'/product/1');await page.getByRole('button',{name:'Add to cart',exact:true}).click();await expect(page.locator('nav .count')).toHaveText('1');
 const email=`buyer-${testInfo.project.name}-${Date.now()}@example.com`;
 await visit(page,'/register');await page.getByLabel('Full name').fill('Browser Customer');await page.getByLabel('Email',{exact:true}).fill(email);await page.getByLabel('Password (8+ characters)').fill('CustomerPass123!');await page.getByRole('button',{name:'Create account',exact:true}).click();
 await expect(page.locator('.success')).toContainText('Registration complete');
 await login(page,email,'wrong');await expect(page.locator('.alert')).toContainText('Invalid email or password');
 await login(page,email,'CustomerPass123!');await expect(page.locator('.hello')).toContainText('Browser Customer');
 await visit(page,'/cart');await expect(page.locator('.cart-row')).toHaveCount(1);
 await page.getByRole('link',{name:'Proceed to checkout'}).click();await expect(page.getByRole('heading',{name:'Checkout',exact:true})).toBeVisible();
 await page.getByRole('button',{name:'Place order',exact:true}).click();await expect(page.locator('.alert')).toContainText('Complete all delivery fields');
 await page.getByLabel('Full name').fill('Browser Customer');await page.getByLabel('Phone number').fill('+77001234567');await page.getByLabel('Delivery address').fill('Astana, Test Street 1');
 await page.getByRole('button',{name:'Place order',exact:true}).click();await expect(page.getByRole('heading',{name:'Thanks for your order!'})).toBeVisible();
 const successUrl=page.url();await expect(page.locator('nav .count')).toHaveText('0');
 await page.getByRole('link',{name:'View my orders'}).click();await expect(page.locator('.order-card')).toHaveCount(1);await expect(page.locator('.order-card')).toContainText('Wireless Headphones');
 await page.reload();await page.locator('main[data-ready="true"]').waitFor();await expect(page.locator('.order-card')).toHaveCount(1);await noOverflow(page);
 await visit(page,'/admin');await expect(page.getByRole('heading',{name:'Access denied'})).toBeVisible();
 await page.getByRole('button',{name:'Logout',exact:true}).click();await expect(page.getByRole('link',{name:'Sign In',exact:true})).toBeVisible();await page.locator('main[data-ready="true"]').waitFor();
 // A second customer's session must not reveal the first customer's order.
 await visit(page,'/register');await page.getByLabel('Full name').fill('Other Customer');await page.getByLabel('Email',{exact:true}).fill('other-'+email);await page.getByLabel('Password (8+ characters)').fill('CustomerPass123!');await page.getByRole('button',{name:'Create account',exact:true}).click();
 await login(page,'other-'+email,'CustomerPass123!');await visit(page,successUrl);await expect(page.getByRole('heading',{name:'Order not found'})).toBeVisible();
 await visit(page,'/orders');await expect(page.locator('.order-card')).toHaveCount(0);
 await visit(page,'/product/99999');await expect(page.getByText('Product not found.')).toBeVisible();
 await page.screenshot({path:`../../artifacts/${testInfo.project.name}-customer.png`,fullPage:true});
 expect(errors).toEqual([]);
});

test('admin journey: categories, products, completed and cancelled orders',async({page},testInfo)=>{
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await login(page,'browser-admin@example.com','BrowserAdmin123!');await visit(page,'/admin');await expect(page.getByRole('heading',{name:'Admin dashboard'})).toBeVisible();await noOverflow(page);await page.screenshot({path:`../../artifacts/${testInfo.project.name}-dashboard.png`,fullPage:true});
 const category='QA '+testInfo.project.name;
 await page.getByRole('link',{name:'Categories'}).click();await page.getByLabel('Category name').fill(category);await page.getByRole('button',{name:'Add category',exact:true}).click();
 let row=page.locator('.admin-item').filter({hasText:category});await expect(row).toHaveCount(1);
 await row.getByRole('button',{name:'Edit',exact:true}).click();await page.getByLabel('Category name').fill(category+' updated');await page.getByRole('button',{name:'Save changes',exact:true}).click();await expect(page.locator('.admin-item').filter({hasText:category+' updated'})).toHaveCount(1);
 await visit(page,'/admin/products');const name='QA product '+testInfo.project.name;
 await page.getByLabel('Name',{exact:true}).fill(name);await page.getByLabel('Description',{exact:true}).fill('Browser tested product');await page.getByLabel(/^Price/).fill('1000');await page.getByLabel('Stock',{exact:true}).fill('4');await page.getByLabel('Category',{exact:true}).selectOption({label:category+' updated'});await page.getByRole('button',{name:'Save product',exact:true}).click();
 row=page.locator('.admin-item').filter({hasText:name});await expect(row).toHaveCount(1);
 await row.getByRole('button',{name:'Edit',exact:true}).click();await page.getByLabel(/^Price/).fill('1200');await page.getByRole('button',{name:'Save product',exact:true}).click();await expect(row).toContainText('1,200');await noOverflow(page);
 await visit(page,'/catalog');await page.getByLabel('Search products').fill(name);await expect(page.locator('.product-card')).toHaveCount(1);
 await page.getByRole('button',{name:'Add to cart',exact:true}).click();await expect(page.locator('nav .count')).toHaveText('1');
 await visit(page,'/checkout');await page.getByLabel('Full name').fill('Test Admin');await page.getByLabel('Phone number').fill('+77001234567');await page.getByLabel('Delivery address').fill('QA address');await page.getByLabel('Payment method').selectOption('Demo Card');await page.getByRole('button',{name:'Place order',exact:true}).click();await expect(page.getByRole('heading',{name:'Thanks for your order!'})).toBeVisible();
 await visit(page,'/admin/orders');const order=page.locator('.order-card').filter({hasText:name});await expect(order).toHaveCount(1);await order.getByRole('button',{name:'Mark Processing',exact:true}).click();await expect(order.locator('.pill')).toHaveText('Processing');await order.getByRole('button',{name:'Cancel',exact:true}).click();await expect(order.locator('.pill')).toHaveText('Cancelled');
 await visit(page,'/admin/products');row=page.locator('.admin-item').filter({hasText:name});await expect(row).toContainText('Stock 4');await row.getByRole('button',{name:'Delete',exact:true}).click();await expect(row).toHaveCount(0);
 await visit(page,'/admin/categories');row=page.locator('.admin-item').filter({hasText:category+' updated'});await row.getByRole('button',{name:'Delete',exact:true}).click();await expect(row).toHaveCount(0);
 await visit(page,'/admin/orders');await expect(page.locator('.order-card').filter({hasText:name})).toContainText('Cancelled');await noOverflow(page);
 await page.screenshot({path:`../../artifacts/${testInfo.project.name}-admin.png`,fullPage:true});expect(errors).toEqual([]);
});

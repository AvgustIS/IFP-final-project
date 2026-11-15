const { defineConfig, devices } = require('@playwright/test');
const path=require('node:path');
const fs=require('node:fs');
const artifacts=path.resolve(__dirname,'../../artifacts');fs.mkdirSync(artifacts,{recursive:true});
module.exports=defineConfig({
 testDir:'.',testMatch:'shop.spec.cjs',fullyParallel:false,workers:1,timeout:60000,
 expect:{timeout:10000},reporter:[['list'],['html',{open:'never'}]],
 use:{baseURL:'http://127.0.0.1:5149',trace:'retain-on-failure',screenshot:'only-on-failure'},
 projects:[
  {name:'edge-desktop',use:{...devices['Desktop Edge'],channel:'msedge'}},
  {name:'edge-mobile',use:{...devices['Pixel 7'],defaultBrowserType:'chromium',channel:'msedge'}},
  {name:'firefox-desktop',use:{...devices['Desktop Firefox']}}
 ],
 webServer:{command:'dotnet run --no-build --project ../../src/EasyShop.Web --no-launch-profile',url:'http://127.0.0.1:5149/health',reuseExistingServer:false,timeout:60000,
  env:{ASPNETCORE_ENVIRONMENT:'Development',ASPNETCORE_URLS:'http://127.0.0.1:5149',ConnectionStrings__Shop:'Data Source='+path.join(artifacts,'browser-'+process.pid+'.db'),EASYSHOP_ADMIN_EMAIL:'browser-admin@example.com',EASYSHOP_ADMIN_PASSWORD:'BrowserAdmin123!'}}
});

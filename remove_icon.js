const fs = require('fs');

let path = 'TurfBooking.Infrastructure/Templates/OtpEmail.html';
let content = fs.readFileSync(path, 'utf8');

const regex = /<td style='padding: 12px 16px; text-align: right; vertical-align: middle; width: 30px;'>\s*<img src='https:\/\/img\.icons8\.com\/material-outlined\/24\/64748b\/copy\.png' width='22' height='22' alt='Copy' style='display: block;' \/>\s*<\/td>/;

content = content.replace(regex, '');
fs.writeFileSync(path, content);
console.log('Removed copy icon from email');

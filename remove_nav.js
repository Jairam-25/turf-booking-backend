const fs = require('fs');

let path = 'TurfBooking.Infrastructure/Templates/OtpEmail.html';
let content = fs.readFileSync(path, 'utf8');

const regex = /<a href="https:\/\/turf-booking-frontend-app\.vercel\.app\/#\/auth\/login\?copy=\{otpCode\}"[^>]*>\s*<img[^>]*>\s*<\/a>/;

const replacement = `<span title="Double-click the number to copy" style="margin-left: 12px; display: inline-block; vertical-align: middle; background: rgba(123, 57, 252, 0.1); padding: 8px; border-radius: 6px; cursor: pointer;">
                                            <img src="https://img.icons8.com/material-outlined/24/7b39fc/copy.png" width="20" height="20" alt="Copy" style="vertical-align: middle; display: block; border: none;" />
                                        </span>`;

content = content.replace(regex, replacement);
fs.writeFileSync(path, content);
console.log('Removed navigation from email copy icon');

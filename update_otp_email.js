const fs = require('fs');

let path = 'TurfBooking.Infrastructure/Templates/OtpEmail.html';
let content = fs.readFileSync(path, 'utf8');

const oldHtml = `<table align='center' cellpadding='0' cellspacing='0' style='margin: 30px auto;'>
                                <tr>
                                    <td style='background-color:#f3e8ff; border: 2px dashed #7b39fc; padding: 15px 40px; border-radius: 8px; text-align: center;'>
                                        <span style='font-size: 32px; font-weight: bold; color: #7b39fc; letter-spacing: 5px;'>{otpCode}</span>
                                    </td>
                                </tr>
                            </table>`;

const newHtml = `<table align='center' cellpadding='0' cellspacing='0' style='margin: 30px auto;'>
                                <tr>
                                    <td style='background-color:#f3e8ff; border: 2px dashed #7b39fc; padding: 15px 40px; border-radius: 8px; text-align: center; position: relative;'>
                                        <span style='font-size: 32px; font-weight: bold; color: #7b39fc; letter-spacing: 5px;'>{otpCode}</span>
                                        <div style='margin-top: 10px; font-size: 12px; color: #7b39fc; font-weight: bold; display: flex; align-items: center; justify-content: center; gap: 4px;'>
                                            <span style='user-select: all;'>📋 Tap/Select to Copy</span>
                                        </div>
                                    </td>
                                </tr>
                            </table>`;

content = content.replace(oldHtml, newHtml);
fs.writeFileSync(path, content);
console.log('Updated OTP Email template');

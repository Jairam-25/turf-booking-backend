const fs = require('fs');
const path = require('path');

// 1. Update User.cs
let userCsPath = 'TurfBooking.Domain/Entities/User.cs';
if (fs.existsSync(userCsPath)) {
    let content = fs.readFileSync(userCsPath, 'utf8');
    if (!content.includes('public string? District { get; set; }')) {
        content = content.replace('public string? State { get; set; }', 
                                  'public string? State { get; set; }\n    public string? District { get; set; }\n    public string? Pincode { get; set; }');
        fs.writeFileSync(userCsPath, content);
        console.log('Updated User.cs');
    }
}

// 2. Update RegisterRequestDto.cs
let regReqDtoPath = 'TurfBooking.Application/DTOs/RegisterRequestDto.cs';
if (fs.existsSync(regReqDtoPath)) {
    let content = fs.readFileSync(regReqDtoPath, 'utf8');
    if (!content.includes('public string District { get; set; }')) {
        content = content.replace('public string City { get; set; } = string.Empty;', 
                                  'public string City { get; set; } = string.Empty;\n        public string State { get; set; } = string.Empty;\n        public string District { get; set; } = string.Empty;\n        public string Pincode { get; set; } = string.Empty;');
        fs.writeFileSync(regReqDtoPath, content);
        console.log('Updated RegisterRequestDto.cs');
    }
}

// 3. Update LoginResponseDto.cs
let loginResDtoPath = 'TurfBooking.Application/DTOs/LoginResponseDto.cs';
if (fs.existsSync(loginResDtoPath)) {
    let content = fs.readFileSync(loginResDtoPath, 'utf8');
    if (!content.includes('public string? District { get; set; }')) {
        content = content.replace('public string? State { get; set; }', 
                                  'public string? State { get; set; }\n        public string? District { get; set; }\n        public string? Pincode { get; set; }');
        fs.writeFileSync(loginResDtoPath, content);
        console.log('Updated LoginResponseDto.cs');
    }
}

// 4. Update UpdateProfileDto.cs
let updateProfileDtoPath = 'TurfBooking.Application/DTOs/UpdateProfileDto.cs';
if (fs.existsSync(updateProfileDtoPath)) {
    let content = fs.readFileSync(updateProfileDtoPath, 'utf8');
    if (!content.includes('public string? District { get; set; }')) {
        content = content.replace('public string? State { get; set; }', 
                                  'public string? State { get; set; }\n        public string? District { get; set; }\n        public string? Pincode { get; set; }');
        fs.writeFileSync(updateProfileDtoPath, content);
        console.log('Updated UpdateProfileDto.cs');
    }
}

// 5. Update AuthController.cs UpdateProfile method
let authControllerPath = 'TurfBooking.API/Controllers/AuthController.cs';
if (fs.existsSync(authControllerPath)) {
    let content = fs.readFileSync(authControllerPath, 'utf8');
    if (!content.includes('user.District = request.District;')) {
        content = content.replace('user.State = request.State;', 
                                  'user.State = request.State;\n            user.District = request.District;\n            user.Pincode = request.Pincode;');
        
        content = content.replace('user.State,\n                user.MaritalStatus,', 
                                  'user.State,\n                user.District,\n                user.Pincode,\n                user.MaritalStatus,');
        fs.writeFileSync(authControllerPath, content);
        console.log('Updated AuthController.cs');
    }
}

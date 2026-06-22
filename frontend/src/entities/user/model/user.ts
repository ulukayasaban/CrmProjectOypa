export interface UserDto {
  id: string;
  email: string;
  fullName: string;
  position: string | null;
  phone: string | null;
  roles: string[];
}

export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  refreshToken: string;
  user: UserDto;
}

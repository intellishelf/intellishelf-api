import { Injectable } from '@nestjs/common';
import { User } from '../models/user.entity';

@Injectable()
export class UsersService {
  private readonly users: User[] = [
    {
      userId: '1',
      userName: 'john',
      password: 'changeme',
    },
    {
      userId: '2',
      userName: 'maria',
      password: 'guess',
    },
  ];

  async findByName(userName: string): Promise<User | undefined> {
    return this.users.find(user => user.userName === userName);
  }
  async findById(userId: string): Promise<User | undefined> {
    return this.users.find(user => user.userId === userId);
  }
}
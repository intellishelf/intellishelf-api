export interface AuthorizedRequest extends Request {
  userId: string;
  headers: Headers & {
    authorization: string;
  }
}

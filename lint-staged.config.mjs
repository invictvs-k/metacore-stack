export default {
  "*.{js,ts,tsx,json,md,yml,yaml}": ["prettier --write"],
  "*.ts?(x)": ["eslint --fix"],
  "*.cs": ["dotnet format server-dotnet/RoomServer.sln --verify-no-changes"]
};

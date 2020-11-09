// MsgClient.cpp : Этот файл содержит функцию "main". Здесь начинается и заканчивается выполнение программы.
//

#include "pch.h"
#include "framework.h"
#include "MsgClient.h"
#include "../MsgServer/Msg.h"
#include <mutex>

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

mutex m1;

void ReceiveMsg(int Id)
{
    CSocket s;
    Message Msg;
    for (;;) {
        Sleep(1000);
        s.Create();
        s.Connect("127.0.0.1", 12345);
        Message::Send(s, M_BROKER, Id, M_GETDATA);
        m1.lock();
        if (Msg.Receive(s) == M_DATA)
            cout << endl << "Получено сообщение от " << Msg.m_Header.m_From << ": " << Msg.m_Data;
        m1.unlock();
        s.Close();
    }
}


void Client()
{
    setlocale(LC_ALL, "Russian");
    AfxSocketInit();
    CSocket Socket;
    Socket.Create();
    Socket.Connect("127.0.0.1", 12345);
    Message Msg;
    int Id;
    Message::Send(Socket, M_BROKER, 0, M_INIT);
    if (Msg.Receive(Socket) == M_INIT)
        Id = Msg.m_Header.m_To;
    Socket.Close();
    thread t(ReceiveMsg, ref(Id));
    t.detach();
    bool MsgType;
    string Data1;
    bool Prov;
    for (;;) {
        cout << "Отправить сообщение - 0; Выйти -1: ";
        cin >> Prov;
        switch (Prov)
        {
        case 0:
        {
            m1.lock();
            cout << "Личное - 0; Широковещательное - 1: ";
            cin >> MsgType;
            cin.ignore(256, '\n');
            cout << "Type message: ";
            getline(cin, Data1, '\n');
            switch (MsgType)
            {
            case 0:
            {
                int IdRec;
                cout << "Введите получателя: ";
                cin >> IdRec;
                Socket.Create();
                Socket.Connect("127.0.0.1", 12345);
                Message::Send(Socket, IdRec, Id, M_DATA, Data1);
                Socket.Close();
                break;
            }
            case 1:
            {
                Socket.Create();
                Socket.Connect("127.0.0.1", 12345);
                Message::Send(Socket, M_ALL, Id, M_DATA, Data1);
                Socket.Close();
                break;
            }
            }
            m1.unlock();
            break;
        }
        case 1:
        {
            Socket.Create();
            Socket.Connect("127.0.0.1", 12345);
            Message::Send(Socket, M_BROKER, Id, M_EXIT);
            Socket.Close();
            return;
        }
        }
    }
}

// Единственный объект приложения

CWinApp theApp;

int main()
{
    int nRetCode = 0;

    HMODULE hModule = ::GetModuleHandle(nullptr);

    if (hModule != nullptr)
    {
        // инициализировать MFC, а также печать и сообщения об ошибках про сбое
        if (!AfxWinInit(hModule, nullptr, ::GetCommandLine(), 0))
        {
            // TODO: вставьте сюда код для приложения.
            wprintf(L"Критическая ошибка: сбой при инициализации MFC\n");
            nRetCode = 1;
        }
        else
        {
            // TODO: вставьте сюда код для приложения.
            Client();
        }
    }
    else
    {
        // TODO: измените код ошибки в соответствии с потребностями
        wprintf(L"Критическая ошибка: сбой GetModuleHandle\n");
        nRetCode = 1;
    }

    return nRetCode;
}

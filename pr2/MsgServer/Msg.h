#pragma once

enum Messages
{
	M_INIT,
	M_EXIT,
	M_GETDATA,
	M_NODATA,
	M_DATA,
	M_CONFIRM
};

enum Members
{
	M_BROKER	= 0,
	M_ALL		= 10,
	M_USER		= 100
};

struct MsgHeader
{
	unsigned __int64 m_To;
	unsigned __int64 m_From;
	unsigned __int64 m_Type;
	unsigned __int64 m_Size;
};

struct Message
{
	MsgHeader m_Header;
	string m_Data;

	Message()
	{
		m_Header = { 0 };
	}

	Message(unsigned int To, unsigned int From, unsigned int Type = M_DATA, const string& Data = "")
		:m_Data(Data)
	{
		m_Header = { To, From, Type, Data.length() };
	}

	void Send(CSocket& s)
	{
		s.Send(&m_Header, sizeof(MsgHeader));
		if (m_Header.m_Size)
		{
			s.Send(m_Data.c_str(), m_Header.m_Size);
		}
	}

	int Receive(CSocket& s)
	{
		s.Receive(&m_Header, sizeof(MsgHeader));
		if (m_Header.m_Size)
		{
			vector <char> v(m_Header.m_Size);
			s.Receive(&v[0], m_Header.m_Size);
			m_Data = string(&v[0], m_Header.m_Size);
		}
		return m_Header.m_Type;
	}

	static void Send(CSocket& s, unsigned int To, unsigned int From, unsigned int Type = M_DATA, const string& Data = "")
	{
		Message m(To, From, Type, Data);
		m.Send(s);
	}
};



using System.ComponentModel;

namespace Operacional.DataBase.Models.DTOs;
/*
public class ControleDocumentoClienteDTO
{
    public int id { get; set; }
    public string item { get; set; }
    public string? quando_enviar { get; set; }
    public string? responsavel_liberacao { get; set; }
    public string? email_responsavel_liberacao { get; set; }
    public int id_documento { get; set; }
    public string sigla { get; set; }
    public string? fecha { get; set; }
    public bool? direcionado_resp { get; set; }
    public string? direcionado_resp_por { get; set; }
    public DateTime? direcionado_resp_em { get; set; }
    public bool? em_analise { get; set; }
    public string? em_analise_por { get; set; }
    public DateTime? em_analise_em { get; set; }
    public bool? concluido { get; set; }
    public string? concluido_por { get; set; }
    public DateTime? concluido_em { get; set; }
    public bool? enviado { get; set; }
    public string? enviado_por { get; set; }
    public DateTime? enviado_em { get; set; }
}
*/

public class ControleDocumentoClienteDTO : INotifyPropertyChanged
{
    DataBaseSettings Setting = DataBaseSettings.Instance;

    private int _id;
    private string _item;
    private string _quando_enviar;
    private string _responsavel_liberacao;
    private string _email_responsavel_liberacao;
    private int _id_documento;
    private string _sigla;
    private string _fecha;
    private bool? _direcionado_resp;
    private string _direcionado_resp_por;
    private DateTime? _direcionado_resp_em;
    private bool? _em_analise;
    private string _em_analise_por;
    private DateTime? _em_analise_em;
    private bool? _concluido;
    private string _concluido_por;
    private DateTime? _concluido_em;
    private bool? _enviado;
    private string _enviado_por;
    private DateTime? _enviado_em;

    public int id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string item
    {
        get => _item;
        set => SetProperty(ref _item, value);
    }

    public string? quando_enviar
    {
        get => _quando_enviar;
        set => SetProperty(ref _quando_enviar, value);
    }

    public string? responsavel_liberacao
    {
        get => _responsavel_liberacao;
        set => SetProperty(ref _responsavel_liberacao, value);
    }

    public string? email_responsavel_liberacao
    {
        get => _email_responsavel_liberacao;
        set => SetProperty(ref _email_responsavel_liberacao, value);
    }

    public int id_documento
    {
        get => _id_documento;
        set => SetProperty(ref _id_documento, value);
    }

    public string sigla
    {
        get => _sigla;
        set => SetProperty(ref _sigla, value);
    }

    public string? fecha
    {
        get => _fecha;
        set => SetProperty(ref _fecha, value);
    }

    public bool? direcionado_resp
    {
        get => _direcionado_resp;
        set
        {
            if (SetProperty(ref _direcionado_resp, value))
            {
                // Quando direcionado_resp muda, atualiza os outros campos
                if (value == true)
                {
                    direcionado_resp_por = Setting.Username; // Ou outro usuário logado
                    direcionado_resp_em = DateTime.Now;
                }
                else
                {
                    direcionado_resp_por = null;
                    direcionado_resp_em = null;
                }
            }
        }
    }

    public string? direcionado_resp_por
    {
        get => _direcionado_resp_por;
        set => SetProperty(ref _direcionado_resp_por, value);
    }

    public DateTime? direcionado_resp_em
    {
        get => _direcionado_resp_em;
        set => SetProperty(ref _direcionado_resp_em, value);
    }

    public bool? em_analise
    {
        get => _em_analise;
        //set => SetProperty(ref _em_analise, value);
        set
        {
            if (SetProperty(ref _em_analise, value))
            {
                // Quando em_analise muda, atualiza os outros campos
                if (value == true)
                {
                    em_analise_por = Setting.Username; // Ou outro usuário logado
                    em_analise_em = DateTime.Now;
                }
                else
                {
                    em_analise_por = null;
                    em_analise_em = null;
                }
            }
        }
    }

    public string? em_analise_por
    {
        get => _em_analise_por;
        set => SetProperty(ref _em_analise_por, value);
    }

    public DateTime? em_analise_em
    {
        get => _em_analise_em;
        set => SetProperty(ref _em_analise_em, value);
    }

    public bool? concluido
    {
        get => _concluido;
        //set => SetProperty(ref _concluido, value);
        set
        {
            if (SetProperty(ref _concluido, value))
            {
                // Quando concluido muda, atualiza os outros campos
                if (value == true)
                {
                    concluido_por = Setting.Username; // Ou outro usuário logado
                    concluido_em = DateTime.Now;
                }
                else
                {
                    concluido_por = null;
                    concluido_em = null;
                }
            }
        }
    }

    public string? concluido_por
    {
        get => _concluido_por;
        set => SetProperty(ref _concluido_por, value);
    }

    public DateTime? concluido_em
    {
        get => _concluido_em;
        set => SetProperty(ref _concluido_em, value);
    }

    public bool? enviado
    {
        get => _enviado;
        //set => SetProperty(ref _enviado, value);
        set
        {
            if (SetProperty(ref _enviado, value))
            {
                // Quando enviado muda, atualiza os outros campos
                if (value == true)
                {
                    enviado_por = Setting.Username; // Ou outro usuário logado
                    enviado_em = DateTime.Now;
                }
                else
                {
                    enviado_por = null;
                    enviado_em = null;
                }
            }
        }
    }

    public string? enviado_por
    {
        get => _enviado_por;
        set => SetProperty(ref _enviado_por, value);
    }

    public DateTime? enviado_em
    {
        get => _enviado_em;
        set => SetProperty(ref _enviado_em, value);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected bool SetProperty<T>(ref T storage, T value, string propertyName = null)
    {
        if (Equals(storage, value))
            return false;

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        return true;
    }
}
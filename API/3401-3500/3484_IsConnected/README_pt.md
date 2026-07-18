# Estratégia IsConnected
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
* **Fonte**: convertido do script MetaTrader 5 `IsConnected.mq5` (pasta `MQL/35056`).
* **Objetivo**: monitora continuamente o status do conector e relata transições on-line/off-line com carimbos de data/hora e durações de tempo de atividade/inatividade.
* **Tipo**: Estratégia de concessionária focada no monitoramento da infraestrutura, e não na execução de ordens.

## Comportamento
1. Quando a estratégia é iniciada, ela registra imediatamente que o módulo de monitoramento foi inicializado e captura o estado atual do conector.
2. Um cronômetro em segundo plano verifica o sinalizador `Connector.IsConnected` a cada `CheckIntervalSeconds` (padrão: 1 segundo).
3. Quando o estado muda, a estratégia:
   * Armazena o momento da transição usando a estratégia `CurrentTime`.
   * Registra o novo estado (`Online` ou `Offline`).
   * Informa quanto tempo durou o estado anterior (tempo on-line antes de uma desconexão ou tempo off-line antes da reconexão).
4. Quando a estratégia para, ela cancela o cronômetro e registra o último estado conhecido para que o operador saiba se a conexão estava ativa ou inativa no desligamento.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
|------|------|---------|-------------|
| `CheckIntervalSeconds` | `int` | `1` | Intervalo (em segundos) entre verificações de conexão sucessivas. Deve ser maior que zero. |

## Detalhes de registro
* Todas as mensagens são escritas com `LogInfo` em inglês para corresponder à implementação MetaTrader que dependia de instruções `Print`.
* Os intervalos de tempo são formatados usando cultura invariável e incluem carimbos de data/hora de início e o tempo gasto no estado anterior.

## Diferenças versus roteiro original
* O loop de espera ocupado de MQL5 é substituído por um temporizador gerenciado que não bloqueia o thread de estratégia.
* Em vez de imprimir linhas de status duplicadas, a versão StockSharp relata mudanças de status estruturadas junto com métricas de tempo de atividade/inatividade.
* A conversão lida com o descarte normal parando o cronômetro em `OnStopped` e `OnReseted`.

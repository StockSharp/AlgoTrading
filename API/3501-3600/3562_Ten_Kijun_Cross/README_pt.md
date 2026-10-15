# Estratégia de alerta cruzado TenKijun (ID 3562)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma versão StockSharp de alto nível API do consultor especialista MetaTrader **TenKijun.mq4**. O EA original apenas observa o indicador Ichimoku e envia notificações push quando o Tenkan-sen (linha de conversão) cruza o Kijun-sen (linha de base). A versão C# mantém a natureza apenas de alerta, mas atualiza a implementação com infraestrutura StockSharp, ligações de gráficos, parametrização e manipulação segura de sessões.

A lógica funciona em velas concluídas de um período configurável. Quando uma nova vela fecha dentro do horário de negociação ativo, a estratégia avalia o indicador Ichimoku calculado com os períodos clássicos de 26/09/52 e registra os últimos valores Tenkan/Kijun. Se o Tenkan cruzar acima do Kijun, uma mensagem informativa será registrada indicando uma linha de alta; se Tenkan cruzar abaixo de Kijun, um alerta de baixa será registrado. Nenhuma negociação é executada – a estratégia destina-se à geração de sinais ou a ser combinada com automação externa.

## Indicador e Fluxo de Dados

- **Indicador** – indicador StockSharp `Ichimoku` com comprimentos Tenkan, Kijun e Senkou Span B parametrizados separadamente. Apenas as linhas Tenkan e Kijun são usadas para tomada de decisão, espelhando o EA original.
- **Assinatura de dados** – Usa `SubscribeCandles` com um `CandleType` configurável. Por padrão, são solicitadas velas de período de 30 minutos.
- **Binding** – `BindEx` é empregado para que o `IchimokuValue` digitado seja entregue ao manipulador sem chamadas manuais para `GetValue`.
- **Gráficos** – Velas e o indicador Ichimoku são anexados automaticamente ao gráfico de estratégia para rápida validação visual de alertas.

## Filtro de Sessão de Negociação

O script MetaTrader restringiu alertas a uma janela de sessão definida pelo usuário. A porta expõe o mesmo recurso por meio de dois parâmetros:

- `StartHour` – início inclusivo da janela ativa (padrão 0). Aceita 0-23.
- `LastHour` – fim inclusivo da janela ativa (padrão 20). Aceita 0-23.

Se `StartHour` for menor ou igual a `LastHour`, os alertas serão produzidos entre essas duas horas do dia. Se o início for maior que o fim, a janela é tratada como noturna (por exemplo, 20 → 6 abrange a sessão do final da noite até o início da manhã).

## Parâmetros

| Parâmetro | Descrição | Padrão | Notas |
|-----------|-------------|---------|-------|
| `StartHour` | Hora em que os alertas podem começar. | 0 | Inclusive, faixa de 0 a 23. |
| `LastHour` | Hora em que os alertas param. | 20 | Inclusive, faixa de 0 a 23. |
| `TenkanPeriod` | Lookback da linha de conversão. | 9 | Otimizável. |
| `KijunPeriod` | Lookback da linha base. | 26 | Otimizável. |
| `SenkouSpanBPeriod` | Lookback do período B inicial. | 52 | Fornecido para ser completo, mesmo que os alertas não dependam da nuvem. |
| `CandleType` | Série de velas usada para o indicador. | Prazo de 30 minutos | Escolha qualquer período baseado em `TimeSpan`. |

## Lógica de Alerta

1. Aguarde a primeira vela finalizada para inicializar o histórico de Tenkan e Kijun.
2. Em cada vela finalizada subsequente dentro da janela de negociação:
   - Extraia os valores Tenkan e Kijun do indicador Ichimoku.
   - Detecte uma linha de alta quando o Tenkan anterior for menor ou igual ao Kijun anterior e o Tenkan atual for maior que o Kijun atual.
   - Detecte uma linha de baixa quando o Tenkan anterior for maior ou igual ao Kijun anterior e o Tenkan atual for menor que o Kijun atual.
   - Emita uma entrada de registro informativa descrevendo a direção, o preço e o carimbo de data/hora da cruz.

## Dicas de uso

- Combine esta estratégia com adaptadores de notificação StockSharp (e-mail, telegrama, som) inscrevendo-se no registro de estratégia ou estendendo o método `ProcessCandle` com código de notificação personalizado.
- Para impulsionar a negociação automatizada, herde de `TenKijunCrossStrategy` e substitua `ProcessCandle` para fazer pedidos em vez de - ou além de - registrar mensagens.
- Ajuste o período da vela para corresponder ao gráfico MetaTrader original usado pelo EA para manter os alertas alinhados.

## Diferenças do original EA

- Usa registro StockSharp em vez de MetaTrader `SendNotification`. O comportamento permanece apenas de alerta, mas depende do pipeline de mensagens da plataforma.
- Adiciona metadados de parâmetros completos (`SetDisplay`, intervalos, sinalizadores de otimização), tornando a estratégia pronta para ferramentas Designer/Optimizer.
- Desenha automaticamente velas e o indicador Ichimoku na janela do gráfico StockSharp quando disponível.

## Arquivos

- `CS/TenKijunCrossStrategy.cs` – implementação principal em C# da lógica de alerta.

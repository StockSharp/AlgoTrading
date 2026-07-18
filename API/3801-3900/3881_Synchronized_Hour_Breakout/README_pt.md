# Estratégia de intervalo de horas sincronizadas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de intervalo de hora sincronizada** é uma versão StockSharp do MetaTrader 4 consultor especialista `JK_sinkhro1`. Ele analisa o equilíbrio das velas de alta e de baixa durante a janela de negociação recente e negocia apenas durante dois horários de sincronização cuidadosamente selecionados (por padrão, 19h e 22h, mais um deslocamento). A estratégia se concentra na captura de rupturas direcionais e, ao mesmo tempo, na aplicação de regras conservadoras de gerenciamento de risco semelhantes às do EA original.

## Lógica de negociação
- Funciona na série de velas selecionada pelo parâmetro `Candle Type` (padrão: velas de 1 hora).
- Mantém uma janela deslizante das últimas `Analysis Period` velas concluídas e conta quantas velas fechadas de alta versus baixa.
- Quando a contagem de alta excede a contagem de baixa, a estratégia se prepara para um longo rompimento durante a primeira hora de sincronização (`22 + Hour Offset`).
- Quando a contagem de baixa excede a contagem de alta, ela se prepara para um pequeno rompimento durante a segunda hora de sincronização (`19 + Hour Offset`).
- Os sinais só são válidos nos primeiros cinco minutos da hora para que a ordem seja sincronizada com a nova barra aberta, como no MQL original.
- Novas negociações serão ignoradas se já houver `Max Active Orders` registrado ou se houver uma posição aberta.

## Gestão de Risco e Gestão Comercial
- As posições são abertas com um tamanho de lote fixo (`Fixed Volume`) ou um tamanho baseado em risco usando o dinheiro da conta e o parâmetro `Risk %`. O modelo de risco divide o risco monetário permitido pela distância de stop em etapas de preço para aproximar o comportamento da fonte EA.
- Cada posição usa três camadas de lógica de saída:
  - Um lucro primário de `Take Profit (pts)` do preço de entrada.
  - Um take-profit secundário mais rápido em `Secondary TP (pts)` para imitar o fechamento manual inicial no código original.
  - Um stop loss rígido em `Stop Loss (pts)` abaixo/acima do preço de entrada.
- Trailing stop opcional: quando o preço avança mais de `Trailing Stop (pts)`, o limite móvel segue o extremo favorável e fecha a posição se o preço recuar além da distância móvel.
- O estado da posição é redefinido após cada saída completa para preparar a próxima janela de sincronização.

## Parâmetros
| Parâmetro | Descrição |
| --- | --- |
| `Take Profit (pts)` | Distância primária de lucro nas etapas de preços de títulos. |
| `Secondary TP (pts)` | Distância de take-profit mais rápida acionada antes do alvo principal. |
| `Stop Loss (pts)` | Distância de stop-loss medida em etapas de preço. |
| `Trailing Stop (pts)` | Distância de parada final; definido como 0 para desabilitar. |
| `Analysis Period` | Número de velas recentes inspecionadas ao contar fechamentos de alta/baixa. |
| `Hour Offset` | Compensação adicionada ao horário de negociação original das 19h e 22h. |
| `Max Active Orders` | Número máximo de ordens ativas simultaneamente permitidas antes que novas entradas sejam bloqueadas. |
| `Fixed Volume` | Volume de negociação usado quando o dimensionamento baseado em risco está desativado. |
| `Use Risk Volume` | Permite o dimensionamento dinâmico da posição com base no caixa do portfólio e na distância do stop. |
| `Risk %` | Porcentagem do caixa do portfólio arriscado por negociação no modo baseado em risco. |
| `Candle Type` | Tipo/período de vela usado para cálculos e geração de sinal. |

## Notas de uso
- A configuração padrão emula a versão MetaTrader que negociou EURUSD durante a sessão de Nova York; ajuste o deslocamento de hora para corresponder ao fuso horário do seu corretor/servidor.
- Certifique-se de que a definição de segurança forneça valores `PriceStep`, `VolumeStep` e `MinVolume` precisos para que o dimensionamento de posição baseado em risco possa alinhar os volumes com os incrementos do lote de troca.
- Como a estratégia depende de dados de fechamento de velas, anexe-a a um provedor de histórico ou feed de dados ao vivo que possa entregar a série de velas selecionada com atraso mínimo.
- A saída final usa preços de fechamento de velas finalizadas, que se aproximam da lógica de rastreamento baseada em ticks da fonte EA, enquanto permanece compatível com o API de alto nível de StockSharp.

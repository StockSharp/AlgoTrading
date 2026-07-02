# Estratégia ASCPlusPlus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **estratégia de breakout ASC++ Williams** transfere o especialista legado MQL4 "ASC++.mq4" para o StockSharp de alto nível do API. A lógica procura faixas de negociação estreitas confirmadas pelo oscilador Williams %R e, em seguida, coloca ordens de stop um pouco além dos extremos da vela. Uma vez acionado, o gerenciamento de risco integrado mantém a posição protegida com take-profit automático, stop loss e comportamento de trailing opcional.

## Como funciona a estratégia

1. **Preparação de indicadores**
   - Osciladores %R rápidos e lentos Williams (padrão 9 e 54 períodos) medem o impulso de curto prazo.
   - Um intervalo verdadeiro médio de 10 períodos suaviza o cálculo do intervalo ponderado "ASC".
   - Os limites dinâmicos `x1 = 67 + RiskLevel` e `x2 = 33 - RiskLevel` imitam as bandas adaptativas originais de sobrecompra/sobrevenda.
2. **Pontuação de sinal**
   - Cada vela finalizada calcula `value2 = 100 - |%R_fast|`. Valores abaixo de `x2` indicam um ambiente de sobrevenda com pressão para romper para cima; valores acima de `x1` sinalizam uma condição de sobrecompra que pode quebrar.
   - Velas consecutivas que ficam dentro dos mesmos contadores de confirmação de incremento extremo. Uma negociação é permitida somente após `SignalConfirmation` barras consecutivas (padrão 5) para aproximar os `SigVal` temporizadores originais.
3. **Colocação de pedido**
   - Quando o filtro de intervalo (`ATR < EntryRange`) confirma a consolidação e o momentum concorda (`%R_fast` acima/abaixo de `%R_slow`), a estratégia coloca uma ordem de stop:
     - Stop de compra em `High + ATR * 0.5 + EntryStopLevel * PriceStep` para quebras de alta.
     - Stop de venda em `Low - ATR * 0.5 - EntryStopLevel * PriceStep` para quebras de baixa.
   - As ordens pendentes do lado oposto são canceladas para evitar exposição conflitante.
4. **Gerenciamento de posição**
   - As ordens de proteção são configuradas via `StartProtection` (takeprofit e stop loss expressos em pontos, rastreamento opcional ativado quando `TrailingStopPoints > 0`).
   - Se um novo sinal entrar em conflito com uma posição existente (por exemplo, um rompimento de alta enquanto vendido), o mecanismo imediatamente nivela a exposição oposta antes de enfileirar a ordem de rompimento, assim como a fonte EA.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | Período de 15 minutos | Fonte de vela base usada para os cálculos. |
| `FastLength` | 9 | Williams %R comprimento usado para o detector de momento rápido. |
| `SlowLength` | 54 | Williams %R comprimento usado para o oscilador de confirmação. |
| `RangeLength` | 10 | Janela de suavização ATR substituindo o loop de intervalo ponderado manual. |
| `EntryStopLevel` | 10 pontos | Compensação extra (em etapas de preço) adicionada às ordens stop de rompimento. |
| `EntryRange` | 27 pontos | Faixa média máxima permitida antes de aceitar uma configuração. |
| `RiskLevel` | 3 | Ajusta os limites de `x1`/`x2`, tornando as faixas de confirmação mais estreitas ou mais largas. |
| `SignalConfirmation` | 5 barras | Número de velas consecutivas que devem permanecer no mesmo extremo antes que uma ordem seja armada. |
| `TakeProfitPoints` | 100 pontos | Distância da ordem de take-profit automática. |
| `StopLossPoints` | 40 pontos | Distância da ordem de stop loss automática. |
| `TrailingStopPoints` | 20 pontos | Ativa o comportamento de rastreamento quando maior que zero. |

## Notas de conversão

- O EA original construiu um ATR ponderado manualmente; a porta StockSharp usa o indicador nativo `AverageTrueRange` com o mesmo lookback de 10 períodos. Isso corresponde à intenção de suavização, evitando buffers personalizados.
- Os temporizadores `SigValBuy` e `SigValSell` no código MQL dependiam de contadores baseados em minutos. A versão C# os emula com `SignalConfirmation` verificações de velas consecutivas para manter a cadência de entrada consistente sem acessar carimbos de data e hora de minuto.
- Pedidos de entrada pendentes são implementados com ajudantes `BuyStop`/`SellStop`. Antes de fazer um novo pedido, o lado oposto é cancelado, espelhando a lógica herdada `OrderDelete`.
- O gerenciamento de stop depende de `StartProtection`, que lida automaticamente com take-profit, stop loss e trailing. Isso cobre a escada final MQL (`TSLevel1`, `TSLevel2`) de uma forma simplificada, mas robusta.
- Todo o acesso aos indicadores ocorre por meio de assinaturas e vinculações de alto nível, conforme exigido pelas diretrizes do projeto – sem chamadas manuais `GetValue` ou caches de indicadores personalizados.

## Dicas de uso

- A estratégia espera instrumentos com um `PriceStep` definido; caso contrário, o padrão é `1`. Ajuste `EntryStopLevel`, `EntryRange` e parâmetros de risco para corresponder ao tamanho do tick do instrumento.
- Reduza `SignalConfirmation` para negociações mais agressivas em prazos mais curtos ou aumente-o para negociar apenas consolidações pronunciadas.
- Considere ativar o desenho do gráfico em um aplicativo host para visualizar as ordens de stop e confirmar se os níveis de rompimento estão alinhados com os máximos/mínimos recentes.
- Sempre teste os dados históricos porque a estratégia é muito sensível a spreads, derrapagens e definições de etapas de preços específicas do corretor.

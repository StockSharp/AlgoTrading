# Estratégia simples de nuvem personalizada WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **WPR Custom Cloud Simple Strategy** é uma versão StockSharp do MetaTrader consultor especialista `WPR Custom Cloud Simple.mq5`. O EA monitora o oscilador %R de Larry Williams e abre negociações quando o indicador sai do território de sobrevenda ou sobrecompra. Esta versão C# mantém o design original de negociação apenas em novas velas, revertendo a posição quando um sinal oposto aparece e evita quaisquer ordens de stop-loss ou take-profit exatamente como a implementação de referência.

## Lógica de negociação
1. Assine o período configurado (`CandleType`) e alimente um indicador `WilliamsR` com as velas recebidas.
2. Espere até que a vela acabe; a estratégia nunca atua em barras incompletas.
3. Armazene os dois últimos valores %R concluídos. Eles espelham as leituras `wpr[1]` e `wpr[2]` de MetaTrader.
4. Gere sinais em cruzamentos:
   - **Configuração longa**: a barra anterior fecha acima de `OversoldLevel` enquanto a barra anterior estava abaixo do nível. Isso recria a condição de "saída de sobrevenda" (`wpr[2] < level` e `wpr[1] > level`) do EA.
   - **Configuração curta**: a barra anterior fecha abaixo de `OverboughtLevel` enquanto a barra anterior estava acima dela, correspondendo à verificação original `wpr[2] > level` e `wpr[1] < level`.
5. Quando uma configuração longa aparecer, nivele qualquer exposição curta e compre um volume líquido. Quando uma configuração curta for acionada, alise o lado comprado e venda um volume líquido. Como StockSharp funciona com posições líquidas, enviar `BuyMarket`/`SellMarket` com `Volume + |Position|` replica perfeitamente o fluxo de fechamento e reversão da conta de hedge de MetaTrader.
6. Nenhuma saída adicional é usada; um novo cruzamento oposto é a única maneira de fechar negociações, assim como no consultor original.

## Parâmetros
| Nome | Tipo | Padrão | MetaTrader contraparte | Descrição |
| --- | --- | --- | --- | --- |
| `WprPeriod` | `int` | `14` | `Inp_WPR_Period` | Comprimento de lookback para o cálculo de Williams %R. |
| `OverboughtLevel` | `decimal` | `-20` | `Inp_WPR_Level1` | Limite que define o território de sobrecompra. Cruzar abaixo desencadeia shorts. |
| `OversoldLevel` | `decimal` | `-80` | `Inp_WPR_Level2` | Limite que define o território de sobrevenda. Cruzar acima desencadeia compras. |
| `CandleType` | `DataType` | Período de 1 hora | `InpWorkingPeriod` | Série de velas usada para atualizar o indicador e avaliar sinais. |
| `Volume` | `decimal` | Volume base da estratégia | `InpLots` | Tamanho do lote para ordens de mercado. A estratégia compensa automaticamente a posição líquida atual antes de abrir uma nova negociação. |

## Diferenças do original EA
- StockSharp opera com posições líquidas. O fechamento da exposição oposta é feito aumentando o volume de ordens de mercado, de forma que o comportamento corresponda ao modelo de hedge sem estruturas contábeis extras como `STRUCT_POSITION`.
- Todas as classes auxiliares de gerenciamento de pedidos (`CTrade`, `CPositionInfo`, verificações de margem, etc.) são substituídas pelos controles de risco integrados do StockSharp. A estratégia depende de `Strategy.Volume` e dos metadados da exchange em vez de cálculos manuais de margem livre.
- O registro é simplificado. A versão StockSharp evita instruções `Print` detalhadas porque o API de alto nível já fornece atualizações de status do pedido.
- As ordens de proteção são omitidas intencionalmente para refletir o design de "fechamento no sinal oposto" da fonte EA.

## Dicas de uso
- Ajuste `CandleType` para o mesmo período de tempo usado em MetaTrader para manter a frequência de cruzamento comparável.
- Williams %R limites são valores negativos. Mover `OverboughtLevel` para mais perto de zero torna as entradas curtas mais raras, enquanto empurrar `OversoldLevel` em direção a `-100` torna as entradas longas mais raras.
- A estratégia assume que `Volume` já está alinhado com o passo mínimo e as regras de compensação do corretor. Ajuste o volume base na interface do usuário ou por meio de código antes de iniciar a negociação ao vivo.

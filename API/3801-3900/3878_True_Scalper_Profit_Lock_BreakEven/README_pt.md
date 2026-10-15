# Estratégia de ponto de equilíbrio True Scalper Profit Lock
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia True Scalper Profit Lock é uma conversão do consultor especialista MetaTrader 4 **TrueScalperProfitLock.mq4**. Ele combina um cruzamento de média móvel exponencial de curto prazo com filtros de polaridade baseados em RSI para entradas de tempo. A estratégia foi projetada para ambientes de scalping de alta frequência, onde as posições são gerenciadas ativamente usando um stop de proteção, um nível fixo de take-profit e um bloqueio de equilíbrio opcional.

## Lógica de negociação

- **Filtro de tendência:** Um EMA de 3 períodos calculado na vela fechada anterior deve ser negociado acima (para compras) ou abaixo (para vendas) de um EMA de 7 períodos da mesma barra. A distância entre as médias deve exceder uma etapa de preço para evitar condições de mercado planas.
- **RSI confirmação:** O EA original oferece dois modos de validação. O método A espera que o período de 2 RSI cruze o limite configurado entre as duas velas fechadas mais recentes. O método B simplesmente verifica se o RSI de duas velas atrás está acima ou abaixo do limite. Ambos os modos podem ser usados ​​de forma independente ou em conjunto, com o Método B habilitado por padrão.
- **Direção da ordem:** As negociações longas exigem que o EMA rápido esteja acima do EMA lento, enquanto o RSI indica condições de sobrevenda (`RSI < threshold`). As negociações curtas refletem a lógica e esperam leituras de sobrecompra.

## Gerenciamento de posição

- **Proteção inicial:** Na entrada, a estratégia calcula um stop-loss e um take-profit de distância fixa usando a etapa de preço do título. Ambos os parâmetros seguem os valores padrão da versão MetaTrader (90 e 44 pontos respectivamente).
- **Bloqueio de lucro:** Quando ativado, o stop loss é movido para o ponto de equilíbrio mais uma compensação configurável quando o preço avança na distância `BreakEvenTriggerPoints`. Isso reflete o comportamento "ProfitLock" do EA original.
- **Temporizadores de abandono:** Dois mecanismos opcionais fecham negociações após um número predefinido de velas concluídas (`AbandonBars`). O Método A inverte a posição imediatamente definindo um sinalizador de entrada oposto, enquanto o Método B apenas fecha e aguarda por novos sinais do indicador.
- **Gerenciamento de dinheiro:** A fórmula de dimensionamento do lote corresponde ao script original: o tamanho da posição é derivado do saldo do portfólio, porcentagem de risco, tipo de conta (mini vs. padrão) e limites de negociação ao vivo. Definir `UseMoneyManagement` como `false` reverte para o parâmetro de volume fixo.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Prazo das velas processadas. |
| `FixedVolume` | Volume base do pedido quando o gerenciamento de dinheiro está desativado. |
| `TakeProfitPoints` / `StopLossPoints` | Meta de lucro e stop protetor nas etapas de preço. |
| `UseRsiMethodA` / `UseRsiMethodB` | Ative os métodos de confirmação RSI correspondentes a EA. |
| `RsiThreshold` | Nível RSI usado por ambos os modos de confirmação. |
| `AbandonMethodA` / `AbandonMethodB` | Habilite as variantes lógicas de abandono. |
| `AbandonBars` | Número de velas concluídas antes que a lógica de abandono seja acionada. |
| `UseMoneyManagement`, `RiskPercent`, `AccountIsMini`, `LiveTradingMode` | Controles de cálculo de volume. |
| `UseProfitLock`, `BreakEvenTriggerPoints`, `BreakEvenOffsetPoints` | Ativação e compensação do ponto de equilíbrio. |
| `MaxOpenTrades` | Número máximo de negociações simultâneas (o comportamento padrão é uma posição aberta). |

## Notas de uso

1. A estratégia avalia apenas velas concluídas para permanecer consistente com o especialista MetaTrader, que depende de lookbacks de barra `shift`.
2. Ative ou desative os métodos RSI para reproduzir a configuração exata usada no modelo original.
3. A lógica de ponto de equilíbrio e abandono depende dos máximos/mínimos das velas para detectar quedas de preços. Ao executar em prazos mais altos, considere o potencial de ultrapassagens intra-barras.
4. A gestão de dinheiro requer uma conexão de portfólio que forneça o `BeginValue`. Se não estiver disponível, a estratégia volta ao volume fixo.

## Arquivos

- `CS/TrueScalperProfitLockBreakEvenStrategy.cs` – Implementação da estratégia em C#.
- `README_zh.md` – documentação chinesa.
- `README_ru.md` – Documentação russa.

# Estratégia Pipsover 8167
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **Pipsover 8167** é uma versão StockSharp do MetaTrader 4 consultor especialista `Pipsover.mq4` distribuído com a compilação 8167. O especialista procura picos fortes do oscilador Chaikin que aparecem imediatamente após um retrocesso para a média móvel simples de 20 períodos na vela anterior. Quando essa combinação acontece, o script abre uma posição na direção do impulso e a protege com distâncias fixas de stop-loss e take-profit (70 e 140 pontos respectivamente no código MQL original). Esta versão C# reconstrói exatamente a mesma lógica usando componentes StockSharp de alto nível para que nenhum acesso direto ao buffer seja necessário.

A implementação usa o indicador Linha de Acumulação/Distribuição (ADL) e duas médias móveis exponenciais para reconstruir os valores do oscilador Chaikin produzidos por `iCustom("Chaikin", ...)` em MetaTrader. Todas as decisões de negociação são atrasadas até que a vela seja totalmente fechada, replicando as verificações `OrdersTotal()` e `Close[1]` / `Open[1]` do script de origem.

## Indicadores e Sinais
- **Média Móvel Simples (SMA 20)** – aplicada ao fechamento de velas. A vela anterior deve perfurar o SMA (baixo abaixo para posições compradas, alto acima para posições vendidas) enquanto mantém um corpo na direção da configuração.
- **Oscilador Chaikin (EMA 3 – EMA 10 do ADL)** – reconstruído internamente a partir do fluxo ADL para espelhar leituras de `iCustom("Chaikin", 0, 0, 1)`. Os limites de entrada e saída são expressos em unidades osciladoras absolutas.
- **Filtro de Ação de Preço** – a estratégia verifica a direção anterior do corpo da vela: os corpos de alta permitem negociações longas, enquanto os corpos de baixa permitem vendas.

## Regras de negociação
### Entrada longa
1. A vela anterior fecha em alta (`Close[1] > Open[1]`).
2. A mínima anterior quebra abaixo do valor SMA20 daquela vela.
3. O valor anterior de Chaikin está abaixo de `-OpenLevel` (padrão 55).
4. Nenhuma posição está aberta no momento.

### Entrada curta
1. A vela anterior fecha em baixa (`Close[1] < Open[1]`).
2. A máxima anterior está acima do valor SMA20 dessa vela.
3. O valor anterior de Chaikin está acima de `OpenLevel`.
4. Nenhuma posição está aberta no momento.

### Condições de saída
- **Posições longas** fecham quando a próxima vela for satisfeita: corpo de baixa, alta acima de SMA20 e Chaikin acima de `CloseLevel` (padrão 90).
- **As posições curtas** fecham quando a próxima vela tem um corpo de alta, abaixo de SMA20, e Chaikin abaixo de `-CloseLevel`.
- Além disso, cada negociação traz um stop protetor em `StopLossPoints` e um take-profit em `TakeProfitPoints`, ambos expressos em etapas de preço do instrumento selecionado.

## Gestão de risco
- Distância de stop-loss: `StopLossPoints × PriceStep` (o padrão é 70 pontos).
- Distância de lucro: `TakeProfitPoints × PriceStep` (o padrão é 140 pontos).
- Tamanho da posição: configurável via `TradeVolume`, mapeado diretamente na propriedade `Volume` da estratégia StockSharp e utilizado para todas as ordens de mercado.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `TradeVolume` | 0,1 | Volume de ordens de mercado (lotes ou contratos, dependendo do título). |
| `MaLength` | 20 | Período do SMA usado para a verificação de pullback. |
| `StopLossPoints` | 70 | Distância de stop-loss medida em etapas de preço. |
| `TakeProfitPoints` | 140 | Distância de lucro medida em etapas de preço. |
| `OpenLevel` | 55 | Limite absoluto do oscilador Chaikin que desbloqueia novas entradas. |
| `CloseLevel` | 90 | Limiar absoluto do oscilador Chaikin que força saídas. |
| `ChaikinFastLength` | 3 | Comprimento EMA rápido na reconstrução de Chaikin. |
| `ChaikinSlowLength` | 10 | Comprimento EMA lento na reconstrução de Chaikin. |
| `CandleType` | H1 | Período usado para assinar velas e calcular indicadores. |

## Notas de implementação
- Velas e indicadores são conectados via `SubscribeCandles().Bind(...)`, portanto a estratégia permanece dentro das diretrizes de alto nível API.
- Os valores Chaikin são calculados na memória alimentando leituras ADL em dois objetos EMA, evitando chamadas proibidas como `GetValue()` em buffers de indicadores.
- As informações anteriores da vela são armazenadas em cache dentro do estado da estratégia para reproduzir o MQL padrão de acesso `Close[1]`, `Low[1]`, `High[1]` e `iCustom(...,1)`.
- Os níveis de stop-loss e take-profit são rastreados manualmente porque o especialista original enviou ordens de mercado simples com compensações estáticas em vez de ordens de proteção do lado do servidor.

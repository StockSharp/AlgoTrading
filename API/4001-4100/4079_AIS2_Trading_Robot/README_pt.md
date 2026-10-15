# AIS2 Trading Robot 20005 (porta StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

AIS2 Trading Robot 20005 é um consultor especialista em breakout intradiário originalmente escrito para MetaTrader 4. A porta recria sua lógica multi-timeframe sobre a estratégia de alto nível de StockSharp API. A estratégia espera por rompimentos de impulso acima/abaixo do ponto médio da vela de período de tempo superior anterior, aplica distâncias dinâmicas de take-profit e stop-loss derivadas do intervalo dessa vela e gerencia posições com um período de tempo secundário e mais rápido que impulsiona um trailing stop.

A conversão centra-se na transparência e no controlo manual: as posições são abertas com ordens de mercado, os níveis de proteção são aplicados dentro da própria estratégia e uma pausa de negociação configurável evita reentradas rápidas. O dimensionamento da posição com base em ações reflete a lógica original de “reserva”, permitindo aos usuários alocar uma fração do valor do portfólio para cada negociação, mantendo um buffer de capital intacto.

## Lógica principal

1. **Análise do timeframe primário** – Em cada vela finalizada do timeframe principal (padrão 15 minutos) a estratégia calcula:
   - Ponto médio da vela `(High + Low) / 2`.
   - Distâncias de take-profit e stop-loss baseadas em intervalo (`range * TakeFactor` e `range * StopFactor`).
   - Aproximação do spread atual, buffers de parada/congelamento e um passo final mínimo.
2. **Condições de rompimento** – As entradas longas exigem um fechamento acima do ponto médio e o preço de venda atual rompendo a máxima anterior mais o spread. Os shorts refletem a condição dos mínimos. As ordens serão bloqueadas se as distâncias de parada/alvo calculadas falharem nas restrições de nível do corretor.
3. **Gerenciamento de risco** – O tamanho da posição é derivado do patrimônio do portfólio: `OrderReserve` define a fração negociável, enquanto `AccountReserve` mantém uma parte intocada. Se o capital disponível ou os limites do corretor não permitirem a negociação, a configuração será ignorada.
4. **Gerenciamento comercial** – O período de tempo mais rápido (padrão 1 minuto) atualiza continuamente a distância final. À medida que o preço avança, o stop migra a favor da negociação, uma vez que a faixa secundária o justifique. Atingir a meta ou o stop resulta em uma saída imediata do mercado.
5. **Proteções operacionais** – Um temporizador de resfriamento (`TradingPauseSeconds`) replica a pausa de negociação MQL original. A estratégia também assina a carteira de pedidos para capturar valores de compra/venda em tempo real; quando indisponível, ele volta para o fechamento da vela.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `PrimaryCandleType` | Prazo maior usado para gerar sinais de entrada. | Velas de 15 minutos |
| `SecondaryCandleType` | Prazo menor para cálculos de trailing stop. | Velas de 1 minuto |
| `TakeFactor` | Multiplicador aplicado ao intervalo da vela primária para construir a distância de lucro. | 1.7 |
| `StopFactor` | Multiplicador aplicado ao intervalo da vela primária para construir a distância de stop-loss. | 1.7 |
| `TrailFactor` | Multiplicador aplicado ao intervalo secundário de velas para atualizações finais. | 0,5 |
| `AccountReserve` | Fração do patrimônio líquido mantida em reserva (não utilizada para negociação). | 0,20 |
| `OrderReserve` | Fração do patrimônio total alocado por negociação antes dos buffers. | 0,04 |
| `BaseVolume` | Volume de negociação de reserva quando o dimensionamento do risco não pode ser calculado. | 1 lote |
| `StopBufferTicks` | Tiques extras adicionados às verificações de conformidade no nível de stop do corretor. | 0 |
| `FreezeBufferTicks` | Carrapatos extras evitando atualizações de paradas frequentes perto dos níveis de congelamento. | 0 |
| `TrailStepMultiplier` | Multiplicador aplicado ao spread ao validar etapas finais. | 1 |
| `TradingPauseSeconds` | Cooldown entre negociações consecutivas. | 5 segundos |

Todos os parâmetros numéricos expõem `SetCanOptimize()` (quando for significativo) para que possam participar de cenários de otimização StockSharp.

## Notas de uso

- Anexe a estratégia a um título e garanta que os dados do nível 1/da carteira de pedidos estejam disponíveis para detecção precisa de spread. Sem cotações ativas, a lógica ainda é executada usando fechamentos de velas, mas as validações de parada tornam-se conservadoras.
- Defina `PrimaryCandleType`/`SecondaryCandleType` para intervalos de tempo existentes em seu feed de dados. A porta usa `SubscribeCandles` e vincula manipuladores por meio do API de alto nível de StockSharp.
- O trailing stop é virtual (gerenciado internamente); nenhuma ordem de parada é enviada ao corretor. Se você precisar de paradas no servidor, estenda o código para registrar ordens de proteção após as entradas.
- `StartProtection()` é chamado na partida para que o motor liquide posições inesperadas, se necessário.

## Diferenças do original EA

- A versão MetaTrader manipulou variáveis globais em todo o terminal; esta porta mantém os parâmetros dentro da estratégia e os expõe por meio de `StrategyParam` wrappers.
- As modificações de ordem foram substituídas por saídas diretas do mercado porque StockSharp lida com a lógica stop/target dentro do próprio algoritmo.
- Os cálculos de risco operam com base no patrimônio do portfólio fornecido por StockSharp em vez de consultas de saldo de conta do MT4.

## Arquivos

- `CS/Ais2TradingRobot20005Strategy.cs` – Implementação de estratégia usando StockSharp API de alto nível.
- `README.md` – Descrição em inglês (este arquivo).
- `README_zh.md` – tradução chinesa.
- `README_ru.md` – Tradução russa.

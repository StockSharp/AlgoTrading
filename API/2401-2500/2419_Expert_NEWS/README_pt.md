# Estratégia Expert NEWS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia Expert NEWS é uma conversão direta do robô MQL5 "Expert_NEWS". A estratégia coloca continuamente ordens stop simétricas acima e abaixo do preço de mercado atual e gerencia as posições resultantes com proteção de break-even, trailing stops e atualizações programadas de ordens pendentes. A implementação depende de cotações Level1 e mantém o volume de negociação padrão em 0.1 lotes.

## Lógica de negociação
1. **Assinatura de cotações** – a estratégia escuta atualizações de melhor bid/ask e calcula preços de ordens a partir dos valores mais recentes.
2. **Ordens stop iniciais** – quando não existe posição comprada ou buy stop ativo, um novo buy stop é colocado em `ask + EntryOffsetTicks * PriceStep`. Quando não existe posição vendida ou sell stop ativo, um sell stop é colocado em `bid - EntryOffsetTicks * PriceStep`.
3. **Atualização de ordens** – a cada `OrderRefreshSeconds`, a estratégia cancela e recria um stop pendente se o preço requerido desviar mais de `TrailingStepTicks` ticks.
4. **Proteção de posição** – após uma execução, a estratégia abre ordens stop de proteção e take-profit se as distâncias solicitadas atenderem à restrição `MinimumStopTicks`.
5. **Controle de break-even** – quando `UseBreakEven` está habilitado, o stop é puxado para `entrada ± BreakEvenProfitTicks` assim que o mercado se move o suficiente e o novo stop respeita a distância mínima da cotação atual.
6. **Trailing stop** – uma vez que o preço avança `TrailingStartTicks`, o stop segue usando `TrailingStopTicks` como distância e `TrailingStepTicks` como passo mínimo de melhoria.
7. **Limpeza** – fechar a posição cancela todas as ordens de proteção restantes.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `StopLossTicks` | Distância inicial do stop de proteção (ticks). Defina como zero para desativar a ordem stop inicial. |
| `TakeProfitTicks` | Distância inicial do take-profit (ticks). Defina como zero para desativar a ordem alvo. |
| `TrailingStopTicks` | Distância do trailing stop (ticks). |
| `TrailingStartTicks` | Lucro em ticks necessário antes de a lógica de trailing ser ativada. |
| `TrailingStepTicks` | Melhoria mínima ao atualizar o trailing stop ou as ordens de entrada pendentes. |
| `UseBreakEven` | Ativa o deslocamento do stop para break-even assim que houver lucro suficiente. |
| `BreakEvenProfitTicks` | Margem de lucro adicional ao mover o stop para break-even. |
| `EntryOffsetTicks` | Distância entre a cotação atual e cada nova ordem stop de entrada. |
| `OrderRefreshSeconds` | Intervalo de tempo entre tentativas automáticas de atualização de ordens stop pendentes. |
| `MinimumStopTicks` | Fallback manual para o requisito de nível de stop do corretor. Stops mais próximos que esta distância não são submetidos. |

## Gestão de posição
- As ordens de proteção sempre correspondem ao volume da posição líquida. Execuções parciais redimensionam automaticamente as ordens stop e take-profit.
- A lógica de break-even e trailing funciona mesmo quando o stop inicial está desativado; o stop será criado dinamicamente assim que as regras forem satisfeitas.
- A estratégia mantém o preço do stop mais recente em memória para que as atualizações de trailing preservem um comportamento monótono.

## Notas de uso
- Certifique-se de que `Security.PriceStep` esteja configurado; cada parâmetro de distância em ticks é multiplicado por este valor.
- O volume padrão é `0.1` para espelhar o robô original. Ajuste a propriedade `Volume` se outro tamanho for necessário.
- `MinimumStopTicks` deve ser definido conforme o requisito de nível de stop da plataforma de negociação, se esta exigir um. Deixe em zero para permitir os stops mais ajustados possíveis.
- O algoritmo não depende de barras históricas e pode operar apenas com cotações em tempo real.

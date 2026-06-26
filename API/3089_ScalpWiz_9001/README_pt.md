# Estratégia ScalpWiz 9001
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
ScalpWiz 9001 é um sistema de scalping de rompimento em camadas que replica o comportamento do consultor especialista MetaTrader do mesmo nome. A estratégia mede o quanto a última vela fecha além do envelope das Bandas de Bollinger e, quando a volatilidade expande bruscamente, implanta uma grade de ordens stop pendentes acima ou abaixo do mercado. O módulo de gerenciamento de dinheiro original é preservado: cada ordem pendente pode usar um lote fixo ou arriscar uma porcentagem configurável do patrimônio da conta.

Uma vez que uma das ordens stop é executada, as ordens restantes são canceladas, enquanto a posição ativa é protegida com um stop-loss tradicional, take-profit e um componente de trailing que só começa a seguir após um buffer adicional ser alcançado. A estratégia é destinada ao scalping de alta frequência em períodos mais baixos, mas pode ser executada em qualquer instrumento suportado pelo StockSharp.

## Lógica de sinal
1. Assinar o período configurado e calcular as Bandas de Bollinger de 20 períodos com fator de desvio `BandsDeviation` (padrão 2).
2. Verificar o quanto o preço de fechamento está da banda superior ou inferior. Quando o fechamento supera a banda em pelo menos a distância do quarto nível (`Level3Pips` convertido para preço), a estratégia se prepara para desaparecer o movimento:
   - Fechamento acima da banda superior → colocar ordens sell-stop abaixo do mercado.
   - Fechamento abaixo da banda inferior → colocar ordens buy-stop acima do mercado.
3. Quatro ordens pendentes são colocadas em distâncias crescentes (`Level0Pips` … `Level3Pips`). Cada ordem usa o volume fixo ou o percentual de risco atribuído àquela camada. As ordens expiram após `ExpirationMinutes` se não forem tocadas.
4. Quando uma ordem de entrada é executada, todas as ordens pendentes são canceladas. A posição executada é gerenciada pelo stop-loss (`StopLossPips`), take-profit (`TakeProfitPips`) e parâmetros de trailing (`TrailingStopPips`, `TrailingStepPips`). O trailing só move o stop de proteção quando o preço percorre pelo menos `TrailingStopPips + TrailingStepPips` desde a entrada.
5. As saídas são executadas com ordens a mercado uma vez que o trailing stop ou o alvo de lucro seja tocado em uma vela completada.

## Parâmetros
- **Candle Type** – período para os cálculos de Bollinger.
- **Bands Period / Bands Deviation** – configuração de Bollinger.
- **Stop Loss (pips)** – distância do stop de proteção em pips.
- **Take Profit (pips)** – distância do alvo de lucro em pips.
- **Trailing Stop (pips)** – distância do trailing stop que segue o movimento após o buffer extra.
- **Trailing Step (pips)** – distância adicional necessária antes de o trailing se ativar.
- **Expiration (minutes)** – vida útil das ordens stop pendentes. Definir como 0 para manter as ordens indefinidamente.
- **Management Mode** – escolher entre `FixedVolume` e `RiskPercent`.
- **Level 0-3 Value** – lote fixo ou percentual de risco para cada camada pendente.
- **Level 0-3 Pips** – deslocamentos de entrada para cada camada pendente.

## Gerenciamento de Dinheiro
Quando `ManagementMode` é igual a `RiskPercent`, a estratégia calcula o volume da ordem a partir do patrimônio da conta e da distância de stop-loss configurada:

```
volume de ordem = (equity × riskPercent / 100) / (stopOffset / priceStep × stepPrice)
```

Se os metadados do mercado (passo de preço, preço de passo ou passo de volume) não estiverem disponíveis, o tamanho da ordem cai para zero por segurança. Com `FixedVolume`, os valores da camada são usados diretamente e arredondados para o passo de volume e limites do instrumento.

## Trailing e Proteção
- Stop-loss e take-profit são inicializados usando distâncias em pips relativas ao preço de execução real.
- A lógica de trailing espelha a implementação do MetaTrader: o stop não é movido até que o preço avance `TrailingStop + TrailingStep`, e depois mantém uma distância de `TrailingStop`.
- As saídas são emitidas como ordens a mercado, garantindo compatibilidade com plataformas que não suportam ordens de proteção do lado do servidor.

## Notas Práticas
- Configurar as distâncias em pips de acordo com o tamanho do tick do instrumento. Para símbolos FX de cinco dígitos, cada pip corresponde a dez passos de preço e a estratégia se ajusta automaticamente inspeccionando os decimais do ativo.
- Como a estratégia depende de ordens stop, verificar os requisitos de nível de stop específicos do corretor e ajustar as distâncias de nível, se necessário.
- O dimensionamento por percentual de risco requer uma avaliação válida do portfólio e metadados de passo do ativo; caso contrário, o volume da ordem será avaliado como zero.
- A estratégia opera em velas completadas e, portanto, reage uma vez por barra, o que suaviza o ruído em comparação com o especialista original baseado em ticks.

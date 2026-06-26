# Estratégia TrendManager TM Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
TrendManager TM Plus é uma estratégia de seguidor de tendência convertida do consultor especialista original do MetaTrader 5 `Exp_TrendManager_Tm_Plus.mq5`. A estratégia depende do indicador personalizado TrendManager, que compara duas médias móveis suavizadas e destaca a distância entre elas. Quando a distância excede um limiar configurável, a estratégia abre posições na direção da tendência prevalecente e fecha posições quando a tendência se reverte ou quando regras de proteção são acionadas.

## Lógica de negociação
1. Construir duas médias móveis na série de velas selecionada. Os métodos de suavização e comprimentos de ambas as linhas são configuráveis.
2. Calcular a distância entre as médias rápida e lenta. Se a distância for maior ou igual ao limiar, o indicador reporta uma tendência de alta. Se a distância for menor ou igual ao limiar negativo, o indicador reporta uma tendência de baixa. Caso contrário, não há sinal acionável.
3. Armazenar os estados de cor (0 para tendência de alta, 1 para tendência de baixa, 3 para neutro) em um breve histórico. O parâmetro `SignalBar` seleciona quantas barras fechadas para trás são avaliadas, seguindo a lógica MQL original.
4. Quando uma nova cor de tendência de alta aparece, a estratégia opcionalmente fecha posições vendidas existentes e pode abrir uma posição comprada se entradas compradas forem permitidas. Por outro lado, uma nova cor de tendência de baixa pode fechar compras e abrir vendas.
5. Saídas opcionais baseadas em tempo e preço fecham operações abertas quando o tempo de detenção excede `MaxPositionAge`, quando o preço cai abaixo de `StopLossDistance` para compras (ou acima para vendas), ou quando `TakeProfitDistance` é atingido.

## Parâmetros
- **Candle Type** – período usado para geração de sinais (padrão: velas de 4 horas para corresponder ao script original).
- **Fast MA Method / Slow MA Method** – algoritmos de suavização para as linhas rápida e lenta. Opções disponíveis: Simple, Exponential, Smoothed, Weighted, Jurik e Kaufman Adaptive.
- **Fast Length / Slow Length** – períodos para as médias móveis.
- **Distance Threshold (`DvLimit`)** – distância absoluta mínima entre as médias rápida e lenta necessária para detectar uma tendência. Converter valores em pontos MT5 em unidades de preço (p.ex., 70 pontos em um símbolo de 5 dígitos ≈ 0.00070).
- **Signal Bar** – número de barras fechadas para trás usadas para confirmar um sinal novo. Um valor de 1 reproduz o comportamento padrão da estratégia MQL.
- **Allow Long Entries / Allow Short Entries** – habilitar ou desabilitar entradas para cada direção.
- **Close Long / Close Short on Opposite Signal** – fechar imediatamente posições abertas quando um sinal da cor oposta aparece.
- **Use Time Exit / Max Position Age** – habilitar e configurar o tempo máximo de detenção antes que uma posição seja fechada à força.
- **Order Volume** – volume fixo enviado com ordens de mercado. Este parâmetro substitui as configurações de gestão de dinheiro da versão MetaTrader.
- **Stop Loss Distance / Take Profit Distance** – deslocamentos de preço de proteção opcionais medidos em unidades de preço absoluto (definir como zero para desabilitar).

## Notas de implementação
- Indicadores StockSharp são usados para reproduzir o comportamento do TrendManager. Modos de suavização exóticos não suportados da biblioteca original recaem para a média móvel StockSharp disponível mais próxima.
- O processamento de sinais mantém um pequeno buffer de histórico para que a verificação de `SignalBar` possa detectar transições assim como o consultor MT5.
- As saídas de proteção são avaliadas em velas completadas. Preenchimentos intrabar do ambiente original são aproximados comparando os máximos e mínimos de velas com as distâncias configuradas.
- Parâmetros específicos de MT5 como `Deviation` e dimensionamento de posição baseado em margem foram substituídos por equivalentes compatíveis com StockSharp.

## Recomendações de uso
1. Escolher um tipo de vela que corresponda ao horizonte de negociação pretendido. H4 é mantido como padrão para paridade com o código fonte.
2. Calibrar o limiar à volatilidade do instrumento. Instrumentos com ticks ou volatilidade maiores requerem valores mais altos.
3. Combinar a saída temporal com distâncias de stop-loss e take-profit para emular os controles de risco do consultor original.
4. Para ativos que negoceiam em ambas as direções, manter ambas as alavancas de entrada habilitadas para que a estratégia possa reverter posições quando a tendência mudar.

## Diferenças em relação ao consultor especialista original
- O dimensionamento de ordens usa um `OrderVolume` fixo em vez do módulo de gestão de dinheiro do MT5.
- Ordens de stop-loss e take-profit são simuladas usando dados de velas em vez de colocação imediata de ordens MT5.
- A estratégia usa as médias móveis nativas do StockSharp. Algumas opções de suavização (p.ex., Jurik, Kaufman adaptive) são mapeadas diretamente, enquanto variantes MT5 não suportadas revertem para a correspondência mais próxima.
- Saídas baseadas em tempo dependem de `MaxPositionAge` com precisão `TimeSpan` em vez de contadores de minutos brutos.

Este documento fornece as informações essenciais necessárias para configurar, executar e estender a estratégia TrendManager TM Plus dentro do ecossistema StockSharp.

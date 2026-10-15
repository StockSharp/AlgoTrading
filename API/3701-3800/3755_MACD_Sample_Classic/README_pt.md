# MACD Exemplo de estratégia clássica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia reproduz o consultor especialista MetaTrader 4 "MACD Exemplo" usando o API de alto nível de StockSharp. Ele negocia em ambas as direções em um único instrumento e reflete a lógica original: faça negociações quando a linha MACD cruza sua linha de sinal no lado correto de zero enquanto uma tendência EMA confirma a direção. As ordens de proteção são convertidas para o gerenciador de risco integrado do StockSharp com trailing stops opcionais.

## Lógica de negociação

1. Aguarde pelo menos 100 velas concluídas para que MACD e EMA contenham histórico suficiente.
2. Calcule um padrão MACD (12, 26, 9) junto com sua linha de sinal e uma média móvel exponencial de 26 períodos que atua como um filtro direcional.
3. **Entrada longa** – permitida apenas quando não existe posição. O MACD deve estar abaixo de zero, mas cruzando acima da linha de sinal, o valor anterior de MACD estava abaixo de seu sinal, o valor absoluto de MACD excede o limite configurável de `MacdOpenLevel` (em faixas de preço) e a tendência EMA está subindo.
4. **Entrada curta** – a configuração simétrica: MACD acima de zero cruzando abaixo de seu sinal, o MACD anterior estava acima do sinal, o valor atual excede o limite `MacdOpenLevel` e a tendência EMA está caindo.
5. **Saída longa** – quando MACD cruza novamente abaixo do sinal no lado positivo de zero e o valor está acima de `MacdCloseLevel`. A posição também pode ser fechada mais cedo pelo trailing stop ou take-profit gerenciado por `StartProtection`.
6. **Saída curta** – quando MACD cruza de volta o sinal no lado negativo e o valor absoluto de MACD excede `MacdCloseLevel`, ou pelos módulos de proteção.

A estratégia nunca mantém mais de uma posição por vez. Cada entrada usa ordens de mercado dimensionadas pela propriedade `Volume`. A lógica de proteção depende do controlador de risco de StockSharp para que as distâncias de lucro e os trailing stops permaneçam sincronizados com o tamanho do tick do instrumento.

## Parâmetros

| Nome | Descrição | Padrão | Notas |
| --- | --- | --- | --- |
| `FastEmaPeriod` | Período EMA rápido usado por MACD. | 12 | Faixa otimizável 6…18.
| `SlowEmaPeriod` | Período EMA lento usado por MACD. | 26 | Faixa otimizável 20…32.
| `SignalPeriod` | Período de sinal EMA dentro de MACD. | 9 | Faixa otimizável 5…13.
| `TrendMaPeriod` | Comprimento EMA para o filtro direcional. | 26 | Faixa otimizável 20…40.
| `MacdOpenLevel` | Limite de entrada expresso em MACD pontos (etapas de preço). | 3 | Equivalente a `MACDOpenLevel` no código MT4.
| `MacdCloseLevel` | Limite de saída expresso em MACD pontos. | 2 | Equivalente a `MACDCloseLevel`.
| `TakeProfitPoints` | Obtenha lucro em faixas de preço (multiplicado pelo tamanho do tick do instrumento). | 50 | Defina como 0 para desativar o lucro.
| `TrailingStopPoints` | Trailing stop em faixas de preço. | 30 | Defina como 0 para desativar o trailing stop.
| `CandleType` | Série de velas usada para atualizações de indicadores. | Período de 5 minutos | Suporta qualquer tipo de vela StockSharp.

## Notas de implementação

- Os indicadores MACD e EMA estão vinculados à assinatura da vela por meio de `BindEx`/`Bind`, permitindo que StockSharp alimente valores prontos para uso sem armazenamento em cache manual.
- As posições são abertas somente quando a plataforma reporta `IsFormedAndOnlineAndAllowTrading()`, evitando negociações enquanto os dados históricos ainda estão sendo carregados ou a conexão está offline.
- Todos os limites que se referem a "pontos" são automaticamente dimensionados pela etapa de preço do instrumento, imitando a constante `Point` de MetaTrader.
- `StartProtection` converte o take-profit fixo e o trailing stop de MetaTrader em ordens de proteção do lado da bolsa. Habilite ou desabilite cada módulo alterando o parâmetro correspondente.
- O registro extensivo (`LogInfo`) documenta cada decisão comercial, simplificando a comparação com o consultor especialista original durante a validação da migração.

## Dicas de uso

- O EA original tem como alvo as principais empresas de Forex em prazos intradiários. Comece com símbolos semelhantes e ajuste os parâmetros se o instrumento usar um tamanho de tick diferente.
- Ao testar símbolos com valores de tick exóticos, verifique se `Security.PriceStep` está configurado; caso contrário, o padrão 1.0 será usado.
- Combine com os recursos de proteção de portfólio do StockSharp se você precisar de gerenciamento de dinheiro no nível da conta além dos limites por posição.

## Etiquetas

- Acompanhamento de tendências
- Momento
- MACD cruzamento
- Intradiário (padrão 5 minutos)
- Trailing Stop + Take Profit

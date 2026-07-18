# Estratégia de Vlado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de reversão de momentum baseada no clássico oscilador Williams %R de Larry Williams. O sistema aguarda que o oscilador alcance leituras extremas de sobrevendido ou sobrecomprado e então reverte a posição na próxima barra concluída. O port do StockSharp mantém o caráter discricionário da implementação original do MetaTrader, expondo cada configuração importante como um parâmetro.

## Visão Geral

- **Categoria**: Estratégia de oscilador de reversão à média.
- **Mercado**: Qualquer instrumento líquido que forneça dados de candles estáveis (pares de forex, futuros de índice, pares spot de criptomoedas).
- **Período**: Configurável via `CandleType`. Padrão: candles de 1 hora, correspondendo ao exemplo de uso original.
- **Direção**: Comprado e vendido. O motor sempre mantém no máximo uma posição e vira quando o sinal oposto aparece.
- **Indicador**: Williams %R com comprimento de lookback e níveis de limiar configuráveis.

## Como Funciona

1. Assina o feed de candles selecionado e calcula Williams %R em cada candle finalizado.
2. Usa o nível padrão de sobrevendido de -75 e nível de sobrecomprado de -25 (os valores são negativos devido à escala do oscilador).
3. Quando %R cai abaixo do nível de sobrevendido, a estratégia entra ou reverte para uma posição comprada.
4. Quando %R sobe acima do nível de sobrecomprado, a estratégia entra ou reverte para uma posição vendida.
5. As ordens são dimensionadas com `Volume + Math.Abs(Position)`, de modo que uma reversão fecha a posição existente e abre a nova em uma única ordem de mercado.
6. Nenhum stop-loss ou take-profit explícito é usado. O risco é controlado pelos níveis do indicador e pelo período escolhido.
7. Cada ação é registrada via `LogInfo`, facilitando a auditoria de negociações na GUI do StockSharp ou nos arquivos de log.

## Parâmetros

- `WilliamsPeriod`: Número de candles usados para calcular o oscilador. Valores mais altos suavizam o sinal, valores mais baixos reagem mais rápido.
- `OverboughtLevel`: Limiar que define quando o mercado é considerado sobrecomprado (padrão -25). Pode ser otimizado.
- `OversoldLevel`: Limiar que define quando o mercado é considerado sobrevendido (padrão -75). Pode ser otimizado.
- `CandleType`: Tipo de candle e período aplicado a todos os cálculos. Funciona com períodos, candles de volume ou barras de range.
- `Volume` (herdado de `Strategy`): Define o tamanho base da ordem. Ajustar ao tamanho da conta e ao apetite de risco.

## Regras de Negociação

- **Entrada comprado**: Acionada quando `%R <= OversoldLevel` e a posição atual está zerada ou vendida.
- **Entrada vendido**: Acionada quando `%R >= OverboughtLevel` e a posição atual está zerada ou comprada.
- **Saída**: Realizada implicitamente pela ordem de reversão quando um sinal oposto aparece.
- **Gestão de posição**: Sempre uma única posição aberta. O algoritmo não faz pirâmide nem saída escalonada.

## Notas Adicionais

- Funciona melhor em mercados laterais ou de tendência lenta onde os osciladores podem oscilar entre extremos.
- Recomenda-se combinar a estratégia com controles de risco externos (stops de patrimônio, filtros de sessão) para negociação ao vivo.
- A implementação inclui renderização de gráficos: a área principal mostra candles e negociações, enquanto um painel secundário plota Williams %R.
- Projetada para pesquisa adicional: cada parâmetro suporta otimização dentro dos otimizadores do StockSharp.

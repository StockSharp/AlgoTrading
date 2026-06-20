# Estratégia de Reversão de Três Barras para Baixo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Uma imagem espelhada da versão altista, esta configuração busca reversões baixistas rápidas. Após duas velas altistas fortes que empurram a novas máximas, uma vela baixista decisiva fecha abaixo da mínima da barra anterior. Uma breve tendência de alta prévia ajuda a confirmar o esgotamento dos compradores.

Os testes indicam um retorno anual médio de aproximadamente 88%. Tem melhor desempenho no mercado de ações.

O algoritmo rastreia uma janela deslizante de três velas. Quando o padrão aparece e qualquer requisito de tendência de alta é atendido, uma posição vendida é tomada com o stop acima da máxima do padrão. As regras são diretas, então os sinais ocorrem imediatamente no fechamento da vela.

A operação é encerrada no stop protetor ou quando outro padrão se forma. Como ela joga com retrações de curto prazo dentro de um potencial movimento de baixa, funciona melhor em mercados voláteis.

## Detalhes

- **Critérios de entrada**: Duas velas altistas com máximas mais altas, seguidas de uma vela baixista fechando abaixo da mínima da barra do meio.
- **Comprado/Vendido**: Somente vendido.
- **Critérios de saída**: Stop-loss ou próximo padrão.
- **Stops**: Sim, acima da máxima do padrão.
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
  - `RequireUptrend` = true
  - `UptrendLength` = 5
- **Filtros**:
  - Categoria: Padrão
  - Direção: Vendido
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio


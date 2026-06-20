# Tweezer Bottom Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Tweezer Bottom é um padrão de reversão de duas velas que aparece após um declínio. Ambas as velas compartilham uma mínima similar, sinalizando que os vendedores falharam em empurrar além daquele nível.

Os testes indicam um retorno anual médio de aproximadamente 184%. Funciona melhor no mercado de criptomoedas.

Esta estratégia entra comprada após a segunda vela confirmar o fundo compartilhado, antecipando um repique à medida que a pressão vendedora se esgota.

Os stops são colocados logo abaixo da mínima comum para gerenciar o risco, e a posição encerra se o preço não conseguir subir.

## Detalhes

- **Critérios de entrada**: correspondência de padrão
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minutos
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

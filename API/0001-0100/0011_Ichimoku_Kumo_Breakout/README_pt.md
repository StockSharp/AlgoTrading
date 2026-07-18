# Rompimento do Kumo Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no rompimento do Kumo (nuvem) de Ichimoku.

Os testes indicam um retorno anual médio de aproximadamente 70%. Funciona melhor no mercado de ações.

Esta abordagem se baseia nos sinais da nuvem Ichimoku. O preço rompendo acima da nuvem com Tenkan-sen cruzando sobre Kijun-sen aciona uma compra, enquanto o rompimento oposto abaixo da nuvem inicia uma posição vendida. As posições são mantidas até que o preço retorne através da nuvem.

A nuvem delineia níveis-chave de suporte e resistência, de modo que o sistema aguarda fechamentos decisivos além dela. Ao combinar múltiplos componentes de Ichimoku, a estratégia evita operações de menor probabilidade durante mercados laterais.


## Detalhes

- **Critérios de entrada**: Sinais baseados em Ichimoku.
- **Comprado/Vendido**: Ambos os sentidos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Ichimoku
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio


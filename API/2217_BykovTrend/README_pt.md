# Estratégia BykovTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o sistema clássico do MetaTrader "Bykov Trend" usando a API de alto nível do StockSharp. O indicador original combina o oscilador Williams %R com um mecanismo simples de detecção de tendência. Quando a tendência muda de baixa para alta, uma posição comprada é aberta. Quando a tendência muda de alta para baixa, uma posição vendida é aberta.

O sistema opera um único instrumento em um período selecionado. Apenas uma posição é mantida por vez; sinais opostos invertem a posição.

## Detalhes

- **Critérios de entrada**  
  - **Comprado**: Williams %R sobe acima de `-K` após estar abaixo de `-100 + K` (`K = 33 - Risk`).  
  - **Vendido**: Williams %R cai abaixo de `-100 + K` após estar acima de `-K`.
- **Comprado/Vendido**: Ambas as direções.  
- **Critérios de saída**  
  - O sinal oposto fecha a posição atual e abre uma nova na direção contrária.  
- **Stops**: Nenhum.  
- **Valores padrão**  
  - `Risk` = 3 (`K = 30`).  
  - `SSP` = 9 (período de retrocesso do Williams %R).  
  - `CandleType` = velas de 1 hora.  
- **Filtros**  
  - Categoria: Seguidor de tendência  
  - Direção: Ambos  
  - Indicadores: Único (Williams %R)  
  - Stops: Não  
  - Complexidade: Simples  
  - Período: Flexível  
  - Sazonalidade: Não  
  - Redes neurais: Não  
  - Divergência: Não  
  - Nível de risco: Médio

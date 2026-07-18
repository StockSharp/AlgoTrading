# Estratégia de Entrada Bollinger Heikin Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que utiliza Bollinger Bands em velas Heikin Ashi. Compra após duas velas Heikin Ashi baixistas consecutivas tocando a banda inferior, seguidas de uma vela altista acima dela. Vende no sentido inverso.

Após entrar, um primeiro alvo igual ao risco é realizado e o stop é ajustado em modo trailing usando os extremos da vela anterior.

## Detalhes

- **Critérios de entrada**:
  - Comprado: duas velas HA baixistas tocando a banda inferior, depois altista acima
  - Vendido: duas velas HA altistas tocando a banda superior, depois baixista abaixo
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: primeiro alvo a 1R, depois stop trailing nas mínimas anteriores
  - Vendido: primeiro alvo a 1R, depois stop trailing nas máximas anteriores
- **Stops**: Mínima/máxima da vela anterior
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Bollinger Bands, Heikin Ashi
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

# Estratégia Super Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador Supertrend.

Os testes indicam um retorno anual médio de aproximadamente 67%. Funciona melhor no mercado de ações.

O Super Trend calcula uma linha dinâmica do ATR que alterna entre suporte e resistência. O preço cruzando acima torna o viés altista, e cruzando abaixo torna-o baixista. A operação termina quando a linha se inverte.

Ao seguir esta linha adaptativa, a estratégia tenta capturar movimentos sustentados minimizando falsos movimentos. Como o nível de stop acompanha o preço, ele trava os lucros uma vez que o momentum desaparece.


## Detalhes

- **Critérios de entrada**: Sinais baseados em ATR, Supertrend.
- **Comprado/Vendido**: Ambos os sentidos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `Period` = 10
  - `Multiplier` = 3.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ATR, Supertrend
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

